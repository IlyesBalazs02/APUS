using APUS.Server.Data;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers
{

	[ApiController]
	[Route("api/[controller]")]
	public class ImagesController : ControllerBase
	{
		private readonly ILogger<ImagesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IStorageService _storageService;

		public ImagesController(
			ILogger<ImagesController> logger,
			IActivityRepository activityRepository,
			IStorageService storageService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
		}

		[HttpPost("{id}/images")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<IActionResult> UploadImages(string id, [FromForm] IFormFileCollection images)
		{
			if (images == null || images.Count() == 0) return BadRequest("No files uploaded");

			// Activity is needed for it's id and userid to know where to save the images
			var activity = await _activityRepository.ReadByIdAsync(id);
			if (activity == null) return NotFound();

			await _storageService.SaveImagesAsync(id, images, activity.UserId);

			return NoContent();
		}

		[HttpGet("{id}")]
		[Authorize]
		[ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<IEnumerable<string>>> GetPictures(string id)
		{
			// Activity is needed for it's id and userid to know the path of the images
			var activity = await _activityRepository.ReadByIdAsync(id);
			if (activity == null) return NotFound();

			var names = _storageService.GetImageFileNames(id, activity.UserId);
			if (!names.Any()) return NotFound();

			var baseUrl = $"{Request.Scheme}://{Request.Host}";

			var urls = names.Select(fn => $"{baseUrl}/Users/{activity.UserId}/Activities/{id}/Images/{fn}");
			return Ok(urls);

		}

		// Path to the activity's trak PNG ( if it exists)
		[HttpGet("{id}/track")]
		[ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status404NotFound)]
		public async Task<ActionResult<IEnumerable<string>>> GetPicture(string id)
		{
			var activity = await _activityRepository.ReadByIdAsync(id);
			if (activity == null) return NotFound();

			var baseUrl = $"{Request.Scheme}://{Request.Host}";
			var url = $"https://localhost:7244/Users/{activity.UserId}/Activities/{id}/ActivityTrackImage.png";
			return Ok(url);
		}
	}

}
