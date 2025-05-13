using APUS.Server.Data;
using APUS.Server.DTOs.GetActivitiesDto;
using APUS.Server.Models;
using APUS.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ActivitiesController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IActivityStorageService _storageService;

		public ActivitiesController(
			ILogger<ActivitiesController> logger,
			IActivityRepository activityRepository,
			IActivityStorageService storageService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
		}

		[HttpPost]
		public async Task<ActionResult<MainActivity>> CreateActivity([FromBody] MainActivity activity)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values
									   .SelectMany(v => v.Errors)
									   .Select(e => e.ErrorMessage);
				return BadRequest(new { errors });
			}

			await _activityRepository.CreateAsync(activity);

			_storageService.CreateActivityFolder(activity.Id);

			return CreatedAtAction(
				nameof(GetById),
				new { id = activity.Id },
				activity);
		}

		[HttpPost("{id}/images")]
		public async Task<IActionResult> UploadImages(string id,[FromForm] IFormFileCollection images)
		{
			if (images == null || images.Count() == 0) return BadRequest("No files uploaded");

			await _storageService.SaveImages(id, images);

			return Ok();
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<MainActivity>>> GetAll()
		{
			var list = await _activityRepository.ReadAllAsync();
			return Ok(list);
		}

		[HttpGet("{id}", Name = nameof(GetById))]
		public async Task<ActionResult<MainActivity>> GetById(string id)
		{
			var act = await _activityRepository.ReadByIdAsync(id);
			if (act == null) return NotFound();
			return Ok(act);
		}

		[HttpGet("get-activities")]
		public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivities()
		{
			var entities = await _activityRepository.ReadAllAsync();
			var dtos = entities.Select(MapToDto);
			return Ok(dtos);
		}

		[HttpPut("{id}")]
		public async Task<IActionResult> EditActivity(string id, [FromBody] MainActivity activity)
		{
			if (id != activity.Id)
				return BadRequest("Mismatched activity ID.");

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
		public async Task<IActionResult> DeleteActivity(string id)
		{
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
			};
		}

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
