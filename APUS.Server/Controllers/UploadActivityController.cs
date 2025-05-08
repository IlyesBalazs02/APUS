using APUS.Server.Controllers.Helpers;
using APUS.Server.Data;
using APUS.Server.Models;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UploadActivityController : ControllerBase
	{
		private readonly string _uploadFolder;
		private readonly string _uploadRoot = @"C:\APUSActivityFiles";
		private readonly ILogger<UploadActivityController> _logger;
		private readonly IActivityRepository _activityRepository;


		public UploadActivityController(IConfiguration config, ILogger<UploadActivityController> logger)
		{
			_uploadFolder = config["GpxSettings:UploadFolder"]
							 ?? Path.Combine("Uploads", "GpxFiles");

			// Ensure folder exists
			var absolutePath = _uploadRoot;
			if (!Directory.Exists(absolutePath))
			{
				Directory.CreateDirectory(absolutePath);
			}

			_logger = logger;
		}

		[HttpPost("upload-activity")]
		public async Task<IActionResult> UploadGpx([FromForm] IFormFile trackFile)
		{
			if (trackFile == null || trackFile.Length == 0)
				return BadRequest("No file provided.");

			var ext = Path.GetExtension(trackFile.FileName).ToLowerInvariant();
			if (ext != ".gpx" && ext != ".tcx")
				return BadRequest("Only GPX or TCX files are allowed.");

			//Build a unique filename
			var fileName = Path.GetRandomFileName() + ext;
			var savePath = Path.Combine(_uploadRoot, fileName);

			//Persist to disk
			await using (var stream = new FileStream(savePath, FileMode.Create))
				await trackFile.CopyToAsync(stream);

			var importedActivity = new ImportActivityModel();

			//create the new activity
			// ext == ".gpx"
			if (ext == ".gpx")
			{
				try
				{
					var gpxHelper = new UploadGPXFileHelper(trackFile);

					importedActivity = gpxHelper.ImportActivity();
				}
				catch (XmlException xmlEx)
				{
					_logger.LogWarning(xmlEx, "GPX XML was malformed");
					return BadRequest("The GPX file is not valid XML.");
				}
				catch (FormatException fmtEx)
				{
					_logger.LogWarning(fmtEx, "GPX contained bad numbers/dates");
					return BadRequest("The GPX file contains invalid numeric or date values.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Unexpected error parsing GPX");
					return StatusCode(500, "An unexpected error occurred while processing your GPX file.");
				}
			}
			else // ext == ".tcx"
			{
				try
				{
					var tcxHelper = new UploadTCXFileHelper(trackFile);

					importedActivity = tcxHelper.ImportActivity();
				}
				catch (XmlException xmlEx)
				{
					_logger.LogWarning(xmlEx, "TCX XML was malformed");
					return BadRequest("The TCX file is not valid XML.");
				}
				catch (FormatException fmtEx)
				{
					_logger.LogWarning(fmtEx, "TCX contained bad numbers/dates");
					return BadRequest("The TCX file contains invalid numeric or date values.");
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Unexpected error parsing TCX");
					return StatusCode(500, "An unexpected error occurred while processing your TCX file.");
				}
			}

			if (importedActivity != null)
			{
				MainActivity newActivity = new MainActivity
				{
					Title = "New Activity",
					Date = importedActivity.StartTime,
					Duration = importedActivity.Duration,
					Calories = importedActivity.TotalCalories,
					AvgHeartRate = importedActivity.AverageHeartRate,
					MaxHeartRate = importedActivity.MaximumHeartRate,


				};
			}


			// Return whatever metadata/URL needed
			return Ok(new
			{
				fileName,
				relativePath = $"/{_uploadFolder}/{fileName}"
			});
		}


	}
}
