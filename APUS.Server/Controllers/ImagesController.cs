using APUS.Server.Data;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
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
		public async Task<IActionResult> UploadImages(string id, [FromForm] IFormFileCollection images)
		{
			if (images == null || images.Count() == 0) return BadRequest("No files uploaded");

			var activity = await _activityRepository.ReadByIdAsync(id);

			if (activity == null) return NotFound();

			await _storageService.SaveImages(id, images, activity.UserId);

			return NoContent();
		}

		[HttpGet("{id}")]
		public async Task<ActionResult<IEnumerable<string>>> GetPictures(string id)
		{
			var activity = await _activityRepository.ReadByIdAsync(id);

			if (activity == null) return NotFound();

			var names = _storageService.GetImageFileNames(id, activity.UserId);
			if (!names.Any()) return NotFound();

			var baseUrl = $"{Request.Scheme}://{Request.Host}";
			//var baseUrl = $"https://localhost:7244";
			var urls = names.Select(fn => $"{baseUrl}/Users/{activity.UserId}/Activities/{id}/Images/{fn}");
			return Ok(urls);

		}

		[HttpGet("{id}/track")]
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
