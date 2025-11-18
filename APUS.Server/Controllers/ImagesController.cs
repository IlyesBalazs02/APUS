using APUS.Server.Core.Helpers;
using APUS.Server.Data.Repositories.Implementations;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client.Extensions.Msal;
using System.Globalization;
using System.Text.Json;

namespace APUS.Server.Controllers
{

	[ApiController]
	[Route("api/[controller]")]
	public class ImagesController : ControllerBase
	{
		private readonly ILogger<ImagesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IStorageService _storageService;
		private readonly IActivityImageRepository _activityImageRepository;
		private readonly IActivityTrackLookupService _activityTrackLookupService;

		public ImagesController(
			ILogger<ImagesController> logger,
			IActivityRepository activityRepository,
			IStorageService storageService,
			IActivityImageRepository activityImageRepository,
			IActivityTrackLookupService activityTrackLookupService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
			_activityImageRepository = activityImageRepository;
			_activityTrackLookupService = activityTrackLookupService;
		}

		[HttpPost("{activityId}/images")]
		public async Task<IActionResult> UploadImages(
	string activityId,
	[FromForm] IFormFileCollection images,
	[FromForm] string? exifJson)
		{
			var userId = User.GetUserId();

			// 1) parse EXIF from client (filename -> ExifMetadataDto)
			var exifDict = string.IsNullOrWhiteSpace(exifJson)
				? new Dictionary<string, ExifMetadataDto>()
				: JsonSerializer.Deserialize<Dictionary<string, ExifMetadataDto>>(exifJson)
				  ?? new Dictionary<string, ExifMetadataDto>();

			// 2) save physical files
			await _storageService.SaveImagesAsync(activityId, images, userId);

			// 3) build ActivityImage entities
			var now = DateTime.UtcNow;
			var baseUrl = $"{Request.Scheme}://{Request.Host}";

			var entities = new List<ActivityImage>();

			foreach (var file in images)
			{
				var fileName = Path.GetFileName(file.FileName);
				var url = $"{baseUrl}/Users/{userId}/Activities/{activityId}/Images/{Uri.EscapeDataString(fileName)}";

				exifDict.TryGetValue(file.FileName, out var metaDto);

				var formats = new[] {
					"yyyy:MM:dd HH:mm:ss",
					"yyyy:MM:dd HH:mm:ssK",
					"yyyy:MM:dd HH:mm:sszzz"
				};

				DateTime? dateTaken = null;

				if (!string.IsNullOrWhiteSpace(metaDto?.dateTaken) &&
					DateTime.TryParseExact(
						metaDto.dateTaken,
						formats,
						CultureInfo.InvariantCulture,
						DateTimeStyles.AssumeLocal,     // or AssumeUniversal
						out var parsed))
				{
					dateTaken = parsed.ToUniversalTime();
				}


				double? gpsLat = null;
				double? gpsLon = null;

				// 4) If we know when the photo was taken, find closest trackpoint in TCX/GPX
				if (dateTaken.HasValue)
				{
					var closest = await _activityTrackLookupService.FindClosestPointAsync(
						activityId,
						userId,
						dateTaken.Value,
						HttpContext.RequestAborted);

					if (closest.HasValue)
					{
						gpsLat = closest.Value.Lat;
						gpsLon = closest.Value.Lon;
					}
				}

				entities.Add(new ActivityImage
				{
					ActivityId = activityId,
					FileName = fileName,
					Url = url,
					UploadedAt = now,
					DateTaken = dateTaken,   // EXIF timestamp
					GpsLat = gpsLat,      // from track
					GpsLon = gpsLon,      // from track
					RawMetadataJson = metaDto != null
						? JsonSerializer.Serialize(metaDto)
						: null
				});
			}

			if (entities.Count > 0)
			{
				await _activityImageRepository.AddRangeAsync(entities);
			}

			return Ok();
		}



		[HttpGet("{id}")]
		[Authorize]
		[ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<IEnumerable<string>>> GetPictures(string id)
		{
			// Activity is needed for it's id and userid to know the path of the images
			var activity = await _activityRepository.ReadByIdAsync(id);

			if (activity == null) return NoContent();

			var names = _storageService.GetImageFileNames(id, activity.UserId);
			// always return 200, even if the array is empty
			var baseUrl = $"{Request.Scheme}://{Request.Host}";
			var urls = names
			  .Select(fn => $"{baseUrl}/Users/{activity.UserId}/Activities/{id}/Images/{fn}")
			  .ToArray();
			return Ok(urls);

		}

		// Path to the activity's trak PNG ( if it exists)
		[HttpGet("{id}/track")]
		[ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public async Task<ActionResult<IEnumerable<string>>> GetTrackImage(string id)
		{
			var activity = await _activityRepository.ReadByIdAsync(id);
			if (activity == null) return NotFound();

			var file = _storageService.ReturnTrackImagePath(activity.Id, activity.UserId);

			//If the trackImage doesnt exist(which can be normal), dont send anything
			if (!System.IO.File.Exists(file))
				return NoContent();   // 204

			var url = $"{Request.Scheme}://{Request.Host}/Users/{activity.UserId}/Activities/{id}/ActivityTrackImage.png";
			return Ok(url);
		}

		[HttpPost("{activityId}/images/delete")]
		public async Task<IActionResult> DeleteImages(string activityId, [FromBody] string[] fileNames)
		{
			var userId = User.GetUserId();

			// 1) delete physical files
			_storageService.DeleteImages(activityId, userId, fileNames);

			// 2) delete metadata records
			await _activityImageRepository.DeleteByFileNamesAsync(activityId, fileNames);

			return NoContent();
		}
	}

}
