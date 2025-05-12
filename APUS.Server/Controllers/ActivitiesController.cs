using APUS.Server.Data;
using APUS.Server.DTOs.GetActivitiesDto;
using APUS.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ActivitiesController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly IActivityRepository _activityRepository;

		public ActivitiesController(ILogger<ActivitiesController> logger, IActivityRepository activityRepository)
		{
			_logger = logger;
			_activityRepository = activityRepository;

		}

		[HttpPost]
		public IActionResult CreateActivity([FromBody] MainActivity activity)
		{
			if (!ModelState.IsValid)
			{
				var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
				return BadRequest(new { errors });
			}

			_activityRepository.Create(activity);

			return Ok();
		}

		[HttpGet]
		public IEnumerable<MainActivity> Get()
		{
			
			return _activityRepository.Read().ToArray();
		}

		[HttpGet("{id}")]
		public MainActivity Get(string id)
		{
			return _activityRepository.Read().SingleOrDefault( t => t.Id == id );
		}

		[HttpGet("get-activities")]
		public IEnumerable<ActivityDto> GetActivities()
		{
			var activities = _activityRepository.Read();
			return activities.Select(MapToDto).ToArray();
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
				
				Running running => CopyBaseProps<RunningActivityDto>(running) with //Running BEFORE the Gps
				{
					DistanceKm = running.TotalDistanceKm,
					ElevationGain = running.TotalAscentMeters,
					Pace = running.AvgPace,
				},
				GpsRelatedActivity Gps => CopyBaseProps<GpsActivityDto>(Gps) with
				{
					DistanceKm = Gps.TotalDistanceKm,
					ElevationGain = Gps.TotalAscentMeters
				},
				_ => CopyBaseProps<ActivityDto>(activity)
			};
		}

	}

}
