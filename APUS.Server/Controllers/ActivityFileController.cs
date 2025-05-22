using APUS.Server.Data;
using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ActivityFileController : ControllerBase
	{
		private readonly ILogger<ActivityFileController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IStorageService _storageService;
		private readonly ITrackpointLoader _loader;
		private readonly ICreateOsmMapPng _createOsmMapPng;
		private readonly Func<string, IActivityImportService> _importerFactory;


		public ActivityFileController(
			ILogger<ActivityFileController> logger,
			IActivityRepository activityRepository,
			IStorageService storageService,
			ITrackpointLoader loader,
			ICreateOsmMapPng createOsmMapPng,
			Func<string, IActivityImportService> importerFactory
			)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
			_loader = loader;
			_createOsmMapPng = createOsmMapPng;
			_importerFactory = importerFactory;
		}

		[HttpPost("upload-activity")]
		[Authorize]
		public async Task<IActionResult> UploadActivityFile([FromForm] IFormFile trackFile)
		{
			if (trackFile == null || trackFile.Length == 0)
				return BadRequest("No file provided.");

			await using var ms = new MemoryStream();
			await trackFile.CopyToAsync(ms);
			ms.Position = 0;

			//Select the correct import service based on the extension
			var ext = Path.GetExtension(trackFile.FileName);
			var importerFactory = _importerFactory(ext);

			try
			{
				var importedActivity = importerFactory.ImportActivity(ms);

				MainActivity newActivity = importedActivity.HasGpsTrack == true
					? new GpsRelatedActivity
					{
						TotalAscentMeters = importedActivity.TotalAscentMeters,
						TotalDescentMeters = importedActivity.TotalDescentMeters,
						TotalDistanceKm = importedActivity.TotalDistanceKm,
						AvgPace = importedActivity.AvgPace
					}
					: new MainActivity();

				newActivity.Title = "Imported Activity";
				newActivity.Date = importedActivity.StartTime;
				newActivity.Duration = importedActivity.Duration;
				newActivity.Calories = importedActivity.TotalCalories;
				newActivity.AvgHeartRate = importedActivity.AverageHeartRate;
				newActivity.MaxHeartRate = importedActivity.MaximumHeartRate;

				var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
				newActivity.UserId = userId;

				//Create the activity into the EF database
				await _activityRepository.CreateAsync(newActivity);

				//Create a folder for the activity in the blob storage
				_storageService.CreateActivityFolder(newActivity.Id, newActivity.UserId);

				//Save the uploaded file into the activity's folder
				await _storageService.SaveTrackAsync(newActivity.Id,newActivity.UserId, trackFile);

				//If the activity has a Track, generate a PNG that will be displayed on the DisplayActivities component
				if (importedActivity.HasGpsTrack) await _createOsmMapPng.GeneratePng(newActivity);


				return CreatedAtRoute(
					routeName: nameof(ActivitiesController.GetById),
					routeValues: new { id = newActivity.Id },
					value: newActivity
				);

			}
			catch (XmlException xmlEx)
			{
				_logger.LogWarning(xmlEx, "Malformed XML in uploaded file");
				return BadRequest("The uploaded file contains invalid XML.");
			}
			catch (FormatException fmtEx)
			{
				_logger.LogWarning(fmtEx, "Invalid data in uploaded file");
				return BadRequest("The uploaded file contains invalid numeric or date values.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing uploaded activity");
				return StatusCode(500, "An unexpected error occurred while processing the file.");
			}
		}

		[HttpGet("{id}")]
		[Authorize]
		public async Task<ActionResult<List<TrackpointDto>>> GetTrackfile(string id)
		{
			var activity = await _activityRepository.ReadByIdAsync(id);

			if (activity == null)
				return NotFound();

			var points = await _loader.LoadTrack(activity);

			if (!points.Any())
				return NotFound();

			return Ok(points);
		}

	}
}
