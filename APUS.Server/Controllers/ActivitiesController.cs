using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Activity;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Implementations.Activity;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.SqlServer.Storage.Internal;
using Newtonsoft.Json;
using OsmSharp.API;
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
		private readonly IActivityService _activityService;

		public ActivitiesController(
			ILogger<ActivitiesController> logger,
			IActivityRepository activityRepository,
			IStorageService storageService,
			IActivityService activityService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
			_activityService = activityService;
		}

		//TODO Create DTO for mainactivity
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

			var dto = MapToDto(act);
			return Ok(dto);
		}

		#region pagedLoading

		[HttpGet("paged")]
		[Authorize]
		public async Task<ActionResult<PagedResponse<ActivityDto>>> GetFeedPaged([FromQuery] int skip = 0, [FromQuery] int take = 10)
		{
			if (take < 1 || take > 50) take = 10;

			var me = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var list = await _activityRepository.GetFeedPagedAsync(me, skip, take + 1);
			var hasMore = list.Count > take;
			if (hasMore) list.RemoveAt(list.Count - 1);

			var items = list.Select(MapToDto).ToList();
			return Ok(new PagedResponse<ActivityDto> { Items = items, HasMore = hasMore });
		}


		[HttpGet("me/paged")]
		[Authorize]
		[ProducesResponseType(typeof(PagedResponse<ActivityDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<PagedResponse<ActivityDto>>> GetMyActivitiesPaged([FromQuery] int skip = 0, [FromQuery] int take = 10)
		{
			if (take < 1 || take > 50) take = 10;

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var list = await _activityRepository.GetByUserIdPagedAsync(userId, skip, take + 1);

			bool hasMore = list.Count > take;
			if (hasMore) list.RemoveAt(list.Count - 1);

			var items = list.Select(MapToDto).ToList();

			return Ok(new PagedResponse<ActivityDto>
			{
				Items = items,
				HasMore = hasMore
			});
		}

		[HttpGet("user/{userId}/paged")]
		[Authorize]
		[ProducesResponseType(typeof(PagedResponse<ActivityDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<PagedResponse<ActivityDto>>> GetUserActivitiesPaged([FromRoute] string userId, [FromQuery] int skip = 0, [FromQuery] int take = 10)
		{
			if (take < 1 || take > 50) take = 10;

			var list = await _activityRepository.GetByUserIdPagedAsync(userId, skip, take + 1);

			bool hasMore = list.Count > take;
			if (hasMore) list.RemoveAt(list.Count - 1);

			var items = list.Select(MapToDto).ToList();

			return Ok(new PagedResponse<ActivityDto>
			{
				Items = items,
				HasMore = hasMore
			});
		}

		#endregion

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

		//ToDo: Pages
		[HttpGet("get-user-activities")]
		[Authorize]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		[ProducesResponseType(typeof(IEnumerable<ActivityDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<IEnumerable<ActivityDto>>> GetUserActivities()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var entities = await _activityRepository.GetActivitiesByUserIdAsync(userId);

			if (entities == null) return NotFound();

			var dtos = entities.Select(MapToDto);
			return Ok(dtos);
		}

		[HttpGet("user/{userId}")]
		[Authorize]
		[ProducesResponseType(typeof(IEnumerable<ActivityDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<IEnumerable<ActivityDto>>> GetActivitiesByUserId(string userId)
		{
			var activities = await _activityRepository.GetActivitiesByUserIdAsync(userId);

			if (activities == null || !activities.Any())
				return Ok(new List<ActivityDto>());

			var dtos = activities.Select(MapToDto).ToList();
			return Ok(dtos);
		}


		[HttpPut("{id}")]
		[Authorize]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status403Forbidden)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> EditActivity(string id, [FromBody] EditActivityRequest request)
		{
			if (id != request.Id)
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
				await _activityService.EditActivityAsync(existing, request);
				return NoContent();
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

		[HttpPost("{id}/like")]
		[Authorize]
		public async Task<ActionResult> ToggleLike(string id)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var updated = await _activityService.ToggleLikeAsync(id, userId);
			if (!updated.HasValue)
				return NotFound();

			return Ok(new { likesCount = updated.Value.likes, isLiked = updated.Value.isLiked });
		}


		private TDto CopyBaseProps<TDto>(MainActivity activity)
			where TDto : ActivityDto, new()
		{
			var avatarUrl  = $"{Request.Scheme}://{Request.Host}{activity.User?.AvatarUrl}" ?? "/Perm/DefaultProfile.png";

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
				LikesCount = activity.LikedBy.Count(),
				IsLikedByCurrentUser = activity.LikedBy.Any(u => u.Id == User.FindFirstValue(ClaimTypes.NameIdentifier)),
				UserFullName = activity.User != null
			? $"{activity.User.FirstName} {activity.User.LastName}"
			: "Unknown",
				avatarUrl = avatarUrl

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
