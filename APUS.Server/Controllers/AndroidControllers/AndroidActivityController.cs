using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Xml;
using APUS.Server.Services.Implementations.FileServices; 

namespace APUS.Server.Controllers.AndroidControllers
{
	[ApiController]
	[Route("api/android/activities")]
	[Authorize]
	public class AndroidActivityController : ControllerBase
	{
		private readonly IActivityRepository _activityRepo;

		private readonly IStorageService _storageService;
		private readonly ICreateOsmMapPng _createOsmMapPng;
		private readonly Func<string, IActivityImportService> _importerFactory;
		private readonly IHuberRegressor _linearAggression;
		private readonly ILogger<AndroidActivityController> _logger;

		public AndroidActivityController(
			IActivityRepository activityRepo,
			IStorageService storageService,
			ICreateOsmMapPng createOsmMapPng,
			Func<string, IActivityImportService> importerFactory,
			IHuberRegressor linearAggression,
			ILogger<AndroidActivityController> logger)
		{
			_activityRepo = activityRepo;

			_storageService = storageService;
			_createOsmMapPng = createOsmMapPng;
			_importerFactory = importerFactory;
			_linearAggression = linearAggression;
			_logger = logger;
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

			var userId = User.GetUserId();

			MainActivity activity = request.ActivityType switch
			{
				"Yoga" => new Yoga(),
				"Bouldering" => new Bouldering(),
				"RockClimbing" => new RockClimbing(),
				"Football" => new Football(),
				"Swimming" => new Swimming(),
				"Tennis" => new Tennis(),

				// fallback
				_ => new MainActivity()
			};

			activity.UserId = userId;

			var startUtc = DateTimeOffset
				.FromUnixTimeSeconds(request.StartTimeUnixSeconds)
				.UtcDateTime;
			activity.Date = startUtc;

			activity.Duration = TimeSpan.FromSeconds(request.DurationSeconds);

			activity.Title = activity.DisplayName ?? activity.ActivityType;

			await _activityRepo.CreateAsync(activity);

			return Ok(new { activityId = activity.Id });
		}

		// imports a recorded GPX/TCX from the Android app
		[HttpPost("gps")]
		public async Task<IActionResult> CreateGps(
			[FromForm] IFormFile trackFile,
			[FromForm] string? activityType)
		{
			if (trackFile == null || trackFile.Length == 0)
				return BadRequest("No file provided.");

			await using var ms = new MemoryStream();
			await trackFile.CopyToAsync(ms);
			ms.Position = 0;

			var ext = Path.GetExtension(trackFile.FileName);
			var importer = _importerFactory(ext);

			try
			{
				var importedActivity = importer.ImportActivity(ms);

				bool hasGps = importedActivity.HasGpsTrack == true;
				var type = string.IsNullOrWhiteSpace(activityType)
					? null
					: activityType.Trim();

				GpsRelatedActivity CreateGps<T>() where T : GpsRelatedActivity, new()
					=> new T
					{
						TotalAscentMeters = importedActivity.TotalAscentMeters,
						TotalDescentMeters = importedActivity.TotalDescentMeters,
						TotalDistanceKm = importedActivity.TotalDistanceKm,
						AvgPace = importedActivity.AvgPace,
						FinishTimeUtc = importedActivity.FinishTimeUtc
					};

				MainActivity CreatePlain<T>() where T : MainActivity, new()
					=> new T();

				MainActivity newActivity;

				if (type is null)
				{
					newActivity = hasGps
						? CreateGps<GpsRelatedActivity>()
						: CreatePlain<MainActivity>();
				}
				else
				{
					newActivity = type switch
					{
						"Running" when hasGps => CreateGps<Running>(),
						"Hiking" when hasGps => CreateGps<Hiking>(),
						"Cycling" when hasGps => CreateGps<Ride>(),
						"GpsRelatedActivity" when hasGps => CreateGps<GpsRelatedActivity>(),

						"MainActivity" => CreatePlain<MainActivity>(),

						_ => hasGps
							? CreateGps<GpsRelatedActivity>()
							: CreatePlain<MainActivity>()
					};
				}

				newActivity.Title = "Imported Activity";
				newActivity.Date = importedActivity.StartTime;
				newActivity.Duration = importedActivity.Duration;
				newActivity.Calories = importedActivity.TotalCalories;
				newActivity.AvgHeartRate = importedActivity.AverageHeartRate;
				newActivity.MaxHeartRate = importedActivity.MaximumHeartRate;

				var userId = User.GetUserId();
				newActivity.UserId = userId;

				// Create the activity in EF
				await _activityRepo.CreateAsync(newActivity);

				// Create folder + save original GPX/TCX
				_storageService.CreateActivityFolder(newActivity.Id, newActivity.UserId);
				var savedTrackPath = await _storageService.SaveTrackAsync(newActivity.Id, newActivity.UserId, trackFile);

				// Generate PNG for feed cards if it has a GPS track
				if (importedActivity.HasGpsTrack)
					await _createOsmMapPng.GeneratePng(newActivity);

				// If it's a Running activity with GPS, train the model
				if (importedActivity.HasGpsTrack &&
					newActivity is Running &&
					(ext.Equals(".tcx", StringComparison.OrdinalIgnoreCase) ||
					 ext.Equals(".gpx", StringComparison.OrdinalIgnoreCase)))
				{
					try
					{
						await _linearAggression.TrainAsync(userId, savedTrackPath);
					}
					catch (Exception laEx)
					{
						_logger.LogError(
							laEx,
							"LinearAggression training failed for user {UserId}, activity {ActivityId}. Track: {TrackPath}",
							userId,
							newActivity.Id,
							savedTrackPath);
					}
				}

				return Ok(new { activityId = newActivity.Id });
			}
			catch (XmlException xmlEx)
			{
				_logger.LogWarning(xmlEx, "Malformed XML in uploaded file (Android GPS)");
				return BadRequest("The uploaded file contains invalid XML.");
			}
			catch (FormatException fmtEx)
			{
				_logger.LogWarning(fmtEx, "Invalid data in uploaded file (Android GPS)");
				return BadRequest("The uploaded file contains invalid numeric or date values.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing uploaded Android GPS activity");
				return StatusCode(500, "An unexpected error occurred while processing the file.");
			}
		}



	}

	public static class UserExtensions
	{
		public static string GetUserId(this ClaimsPrincipal user)
			=> user.FindFirstValue(ClaimTypes.NameIdentifier)
			   ?? throw new InvalidOperationException("No user id");
	}
}
