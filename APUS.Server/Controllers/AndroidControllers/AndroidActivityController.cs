using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace APUS.Server.Controllers.AndroidControllers
{
	[ApiController]
	[Route("api/android/activities")]
	[Authorize]
	public class AndroidActivityController : ControllerBase
	{
		private readonly IActivityRepository _activityRepo;

		public AndroidActivityController(IActivityRepository activityRepo)
		{
			_activityRepo = activityRepo;
		}

		[HttpPost("nongps")]
		public async Task<IActionResult> CreateNonGps([FromBody] NonGpsActivityUploadRequest request)
		{
			if (request == null)
				return BadRequest("Missing body.");

			if (string.IsNullOrWhiteSpace(request.ActivityType))
				return BadRequest("ActivityType is required.");

			if (request.DurationSeconds <= 0)
				return BadRequest("DurationSeconds must be > 0.");

			var userId = User.GetUserId(); // helper below

			// map string to concrete subclass
			MainActivity activity = request.ActivityType switch
			{
				"Yoga" => new Yoga(),
				"Bouldering" => new Bouldering(),
				"RockClimbing" => new RockClimbing(),
				"Football" => new Football(),
				"Swimming" => new Swimming(),
				"Tennis" => new Tennis(),

				// fallback: generic MainActivity if unknown
				_ => new MainActivity()
			};

			activity.UserId = userId;

			// convert Unix seconds to DateTime (UTC)
			var startUtc = DateTimeOffset.FromUnixTimeSeconds(request.StartTimeUnixSeconds).UtcDateTime;
			activity.Date = startUtc;

			// duration
			activity.Duration = TimeSpan.FromSeconds(request.DurationSeconds);

			// optional: can set Title to something simple
			activity.Title = activity.DisplayName ?? activity.ActivityType;

			await _activityRepo.CreateAsync(activity); // repo already exists :contentReference[oaicite:2]{index=2}

			// return ID so Android could use it later if needed
			return Ok(new { activityId = activity.Id });
		}
	}

	public static class UserExtensions
	{
		public static string GetUserId(this ClaimsPrincipal user)
			=> user.FindFirstValue(ClaimTypes.NameIdentifier)
			   ?? throw new InvalidOperationException("No user id");
	}
}
