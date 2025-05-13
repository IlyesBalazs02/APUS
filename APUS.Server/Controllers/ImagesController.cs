using APUS.Server.Data;
using APUS.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers
{

	[ApiController]
	[Route("api/[controller]")]
	public class ImagesController : ControllerBase
	{
		private readonly ILogger<ImagesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly IActivityStorageService _storageService;

		public ImagesController(
			ILogger<ImagesController> logger,
			IActivityRepository activityRepository,
			IActivityStorageService storageService)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_storageService = storageService;
		}

		[HttpPost("{id}/images")]
		public async Task<IActionResult> UploadImages(string id, [FromForm] IFormFileCollection images)
		{
			if (images == null || images.Count() == 0) return BadRequest("No files uploaded");

			await _storageService.SaveImages(id, images);

			return NoContent();
		}
	}
}
