using APUS.Server.Controllers.Helpers;
using APUS.Server.Data;
using APUS.Server.Models;
using APUS.Server.Services;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UploadActivityController : ControllerBase
	{
		private readonly ILogger<UploadActivityController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IActivityStorageService _storageService;
		private readonly string _uploadRoot;

		public UploadActivityController(
			IConfiguration config,
			ILogger<UploadActivityController> logger,
			IActivityRepository activityRepository,
			IActivityStorageService storageService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;

			_uploadRoot = config["GpxSettings:UploadFolder"]
				?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads", "GpxFiles");
			Directory.CreateDirectory(_uploadRoot);
		}

		[HttpPost("upload-activity")]
		public async Task<IActionResult> UploadGpx([FromForm] IFormFile trackFile)
		{
			if (trackFile == null || trackFile.Length == 0)
				return BadRequest("No file provided.");

			var ext = Path.GetExtension(trackFile.FileName).ToLowerInvariant();
			if (ext != ".gpx" && ext != ".tcx")
				return BadRequest("Only GPX or TCX files are allowed.");

			var fileName = Path.GetRandomFileName() + ext;
			var savePath = Path.Combine(_uploadRoot, fileName);

			try
			{
				// Save file to disk
				await using (var stream = new FileStream(savePath, FileMode.Create))
				{
					await trackFile.CopyToAsync(stream);
				}

				// Parse file
				var importedActivity = ext == ".gpx"
					? new UploadGPXFileHelper(trackFile).ImportActivity()
					: new UploadTCXFileHelper(trackFile).ImportActivity();

				// Map to domain model
				MainActivity newActivity = importedActivity.HasGpsTrack == true
					? new GpsRelatedActivity
					{
						TotalAscentMeters = importedActivity.TotalAscentMeters,
						TotalDescentMeters = importedActivity.TotalDescentMeters,
						TotalDistanceKm = importedActivity.TotalDistanceKm,
						AvgPace = importedActivity.AvgPace
					}
					: new MainActivity();

				// Common fields
				newActivity.Title = "Imported Activity";
				newActivity.Date = importedActivity.StartTime;
				newActivity.Duration = importedActivity.Duration;
				newActivity.Calories = importedActivity.TotalCalories;
				newActivity.AvgHeartRate = importedActivity.AverageHeartRate;
				newActivity.MaxHeartRate = importedActivity.MaximumHeartRate;

				// Persist entity
				await _activityRepository.CreateAsync(newActivity);

				// Ensure image folder exists
				_storageService.CreateActivityFolder(newActivity.Id);

				// Return result
				return Ok(new
				{
					Id = newActivity.Id,
					FileName = fileName,
					FilePath = $"/Uploads/GpxFiles/{fileName}"
				});
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
	}
}
