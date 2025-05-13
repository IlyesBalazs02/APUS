using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class GPXController : ControllerBase
	{
		private readonly string _uploadFolder;
		private readonly string path = @"C:\APUSGpxFiles";

		public GPXController(IConfiguration config)
		{
			_uploadFolder = config["GpxSettings:UploadFolder"]
							 ?? Path.Combine("Uploads", "GpxFiles");

			// Ensure folder exists
			var absolutePath = path;
			if (!Directory.Exists(absolutePath))
			{
				Directory.CreateDirectory(absolutePath);
			}
		}

		/*[HttpPost("upload-gpx")]
		public async Task<IActionResult> UploadGpx([FromForm] IFormFile gpxFile)
		{
			if (gpxFile == null || gpxFile.Length == 0)
				return BadRequest("No GPX file provided.");

			// Sanitize and generate a unique filename
			var fileName = Path.GetRandomFileName() + Path.GetExtension(gpxFile.FileName);
			var savePath = Path.Combine(path, fileName);

			using var stream = new FileStream(savePath, FileMode.Create);
			await gpxFile.CopyToAsync(stream);

			return Ok(new { fileName, relativePath = $"/{_uploadFolder}/{fileName}" });
		}*/
	}
}

