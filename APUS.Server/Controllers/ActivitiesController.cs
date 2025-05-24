using APUS.Server.Data;
using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Security.Claims;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ActivitiesController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IStorageService _storageService;

		public ActivitiesController(
			ILogger<ActivitiesController> logger,
			IActivityRepository activityRepository,
			IStorageService storageService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
		}

		[HttpPost]
		[Authorize]
		[ProducesResponseType(typeof(MainActivity), StatusCodes.Status201Created)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<ActionResult<MainActivity>> CreateActivity([FromBody] MainActivity activity)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
									   .SelectMany(v => v.Errors)
									   .Select(e => e.ErrorMessage);
				return BadRequest(new { errors });
			}

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			activity.UserId = userId;

			//Create the activity into the EF database
			await _activityRepository.CreateAsync(activity);

			//Create a folder for the activity in the blob storage
			_storageService.CreateActivityFolder(activity.Id, activity.UserId);

			return CreatedAtAction(
				nameof(GetById),
				new { id = activity.Id },
				activity);
		}

		[HttpGet("{id}", Name = nameof(GetById))]
		[Authorize]
		[ProducesResponseType(typeof(MainActivity), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<MainActivity>> GetById(string id)
		{
			var act = await _activityRepository.ReadByIdAsync(id);

			if (act == null) return NotFound();
			return Ok(act);
		}

		//ToDo: Pages
		[HttpGet("get-activities")]
		[Authorize]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(IEnumerable<ActivityDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities()
		{
			var entities = await _activityRepository.ReadAllAsync();

			if (entities == null) return NotFound();

			var dtos = entities.Select(MapToDto);
			return Ok(dtos);
		}

		[HttpPut("{id}")]
		[Authorize]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> EditActivity(string id, [FromBody] MainActivity activity)
		{
			if (id != activity.Id)
				return BadRequest("Mismatched activity ID.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var existing = await _activityRepository.ReadByIdAsync(id);
			if (existing == null)
				return NotFound();

			if (existing.UserId != userId)
				return Forbid();

			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
									   .SelectMany(v => v.Errors)
									   .Select(e => e.ErrorMessage);
				return BadRequest(new { errors });
			}

			try
			{
				await _activityRepository.UpdateAsync(id, activity);
				return NoContent();
			}
			catch (ValidationException vex)
			{
				return BadRequest(vex.ValidationResult);
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating activity {ActivityId}", id);
				return StatusCode(500, "An unexpected error occurred.");
			}
		}

		[HttpDelete("{id}")]
		[Authorize]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DeleteActivity(string id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var existing = await _activityRepository.ReadByIdAsync(id);
			if (existing == null)
				return NotFound();

			if (existing.UserId != userId)
				return Forbid();

			try
			{
				await _activityRepository.DeleteAsync(id);
				return NoContent();
			}
			catch (KeyNotFoundException)
			{
				return NotFound();
			}
		}

		[HttpGet("{id}/likes")]
		public async Task<ActionResult<int>> GetLikesNo(string id) 
		{
			var activity = await _activityRepository.ReadByIdAsync(id);

			if (activity == null)return NotFound();

			return activity.LikedBy.Count();
		}

		private TDto CopyBaseProps<TDto>(MainActivity activity)
			where TDto : ActivityDto, new()
		{
			return new TDto
			{
				Id = activity.Id,
				Title = activity.Title,
				Description = activity.Description,
				Duration = activity.Duration,
				Date = activity.Date,
				AvgHr = activity.AvgHeartRate,
				TotalCalories = activity.Calories,
				Type = activity.GetType().Name,
				DisplayName = activity.DisplayName,
				LikesCount = activity.LikedBy.Count()

			};
		}

		//Define which values to send to the DisplayActivities component
		private ActivityDto MapToDto(MainActivity activity)
		{
			return activity switch
			{
				Running running => CopyBaseProps<RunningActivityDto>(running) with
				{
					DistanceKm = running.TotalDistanceKm,
					ElevationGain = running.TotalAscentMeters,
					Pace = running.AvgPace,
				},
				GpsRelatedActivity gps => CopyBaseProps<GpsActivityDto>(gps) with
				{
					DistanceKm = gps.TotalDistanceKm,
					ElevationGain = gps.TotalAscentMeters
				},
				_ => CopyBaseProps<ActivityDto>(activity)
			};
		}
	}

}
