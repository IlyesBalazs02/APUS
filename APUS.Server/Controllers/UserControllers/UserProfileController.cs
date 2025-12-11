using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.User;
using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OsmSharp.API;
using System.Security.Claims;

namespace APUS.Server.Controllers.UserControllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UserProfileController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly UserManager<SiteUser> _userMgr;

		public enum TrainingPeriod
		{
			LastWeek,
			LastMonth,
			LastYear
		}

		public UserProfileController(ILogger<ActivitiesController> logger, IActivityRepository activityRepository, UserManager<SiteUser> userMgr)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_userMgr = userMgr;
		}

		[HttpGet("me")]
		[Authorize]
		public async Task<ActionResult<ProfileDto>> GetMyProfile()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var user = await _userMgr.FindByIdAsync(userId);

			if (user == null)
				return NotFound($"User doesnt exist with id:{userId}");

			//ONLY TEMPORARY
			var name = $"{user.FirstName} {user.LastName}";

			return Ok(new ProfileDto { Name = name });
		}

		[HttpGet("{id}", Name = "GetUserProfile")]
		[Authorize]
		public async Task<ActionResult<ProfileDto>> GetUserProfile(string id)
		{
			var userId = id;

			var user = await _userMgr.FindByIdAsync(userId);

			if (user == null)
				return NotFound();

			var name = $"{user.FirstName} {user.LastName}";

			return Ok(new ProfileDto { Name = name });
		}

		private string GetCurrentUserId()
		{
			return User.FindFirstValue(ClaimTypes.NameIdentifier)
				   ?? throw new InvalidOperationException("User not authenticated.");
		}

		private (DateTime fromUtc, DateTime toUtc) GetRangeForPeriod(TrainingPeriod period)
		{
			// Use UTC dates with date-only precision
			var todayUtc = DateTime.UtcNow.Date;

			DateTime fromUtc = period switch
			{
				TrainingPeriod.LastWeek => todayUtc.AddDays(-7),
				TrainingPeriod.LastMonth => todayUtc.AddMonths(-1),
				TrainingPeriod.LastYear => todayUtc.AddYears(-1),
				_ => todayUtc.AddDays(-7)
			};

			// Exclusive upper bound = start of "tomorrow"
			var toUtc = todayUtc.AddDays(1);

			return (fromUtc, toUtc);
		}

		[HttpGet("me/training-time")]
		[ProducesResponseType(typeof(TrainingTimeSummaryDto), StatusCodes.Status200OK)]
		public async Task<ActionResult<TrainingTimeSummaryDto>> GetMyTrainingTime(
			[FromQuery] TrainingPeriod period = TrainingPeriod.LastWeek)
		{
			var userId = GetCurrentUserId();
			return await GetTrainingTimeInternal(userId, period);
		}

		[HttpGet("{userId}/training-time")]
		[ProducesResponseType(typeof(TrainingTimeSummaryDto), StatusCodes.Status200OK)]
		public async Task<ActionResult<TrainingTimeSummaryDto>> GetUserTrainingTime(
			string userId,
			[FromQuery] TrainingPeriod period = TrainingPeriod.LastWeek)
		{
			// Optional: add privacy checks here (e.g., cannot see if profile is private)
			return await GetTrainingTimeInternal(userId, period);
		}

		private async Task<ActionResult<TrainingTimeSummaryDto>> GetTrainingTimeInternal(
	string userId,
	TrainingPeriod period)
		{
			var (fromUtc, toUtc) = GetRangeForPeriod(period);

			var activities = await _activityRepository
				.GetByUserIdAndDateRangeAsync(userId, fromUtc, toUtc);

			var totalHours = activities.Sum(a => a.Duration.TotalHours);
			var count = activities.Count;

			// NEW: per-sport breakdown
			var sports = activities
				.GroupBy(a => a.ActivityType?.ToString() ?? "Unknown")
				.Select(g => new TrainingSportSummaryDto
				{
					ActivityType = g.Key,
					TotalHours = g.Sum(a => a.Duration.TotalHours),
					ActivityCount = g.Count()
				})
				.OrderByDescending(s => s.TotalHours)
				.ToList();

			var dto = new TrainingTimeSummaryDto
			{
				UserId = userId,
				Period = period.ToString(),
				FromUtc = fromUtc,
				ToUtc = toUtc,
				TotalHours = totalHours,
				ActivityCount = count,
				Sports = sports
			};

			return Ok(dto);
		}


		[HttpGet("me/calendar")]
		[ProducesResponseType(typeof(ActivityCalendarMonthDto), StatusCodes.Status200OK)]
		public async Task<ActionResult<ActivityCalendarMonthDto>> GetMyCalendar(
			[FromQuery] int? year,
			[FromQuery] int? month)
		{
			var userId = GetCurrentUserId();
			return await GetCalendarInternal(userId, year, month);
		}

		[HttpGet("{userId}/calendar")]
		[ProducesResponseType(typeof(ActivityCalendarMonthDto), StatusCodes.Status200OK)]
		public async Task<ActionResult<ActivityCalendarMonthDto>> GetUserCalendar(
			string userId,
			[FromQuery] int? year,
			[FromQuery] int? month)
		{
			return await GetCalendarInternal(userId, year, month);
		}

		private async Task<ActionResult<ActivityCalendarMonthDto>> GetCalendarInternal(
	string userId,
	int? year,
	int? month)
		{
			var nowUtc = DateTime.UtcNow;

			int y = year ?? nowUtc.Year;
			int m = month ?? nowUtc.Month;

			var activities = await _activityRepository.GetByUserIdAndMonthAsync(userId, y, m);

			var days = activities
				.GroupBy(a => a.Date.Date)
				.Select(g => new ActivityCalendarDayDto
				{
					Day = g.Key.Day,                               // <– only the day number
					TotalHours = g.Sum(a => a.Duration.TotalHours),
					ActivityCount = g.Count()
				})
				.OrderBy(d => d.Day)
				.ToList();

			var dto = new ActivityCalendarMonthDto
			{
				UserId = userId,
				Year = y,
				Month = m,
				Days = days
			};

			return Ok(dto);
		}


	}
}
