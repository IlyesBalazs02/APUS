using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class UploadActivityController : ControllerBase
	{
		private readonly string _uploadFolder;
		private readonly string _uploadRoot = @"C:\APUSActivityFiles";

		public UploadActivityController(IConfiguration config)
		{
			_uploadFolder = config["GpxSettings:UploadFolder"]
							 ?? Path.Combine("Uploads", "GpxFiles");

			// Ensure folder exists
			var absolutePath = _uploadRoot;
			if (!Directory.Exists(absolutePath))
			{
				Directory.CreateDirectory(absolutePath);
			}
		}

		[HttpPost("upload-activity")]
		public async Task<IActionResult> UploadGpx([FromForm] IFormFile trackFile)
		{
			if (trackFile == null || trackFile.Length == 0)
				return BadRequest("No file provided.");

			var ext = Path.GetExtension(trackFile.FileName).ToLowerInvariant();
			if (ext != ".gpx" && ext != ".tcx")
				return BadRequest("Only GPX or TCX files are allowed.");

			// 1) Build a unique filename
			var fileName = Path.GetRandomFileName() + ext;
			var savePath = Path.Combine(_uploadRoot, fileName);

			// 2) Persist to disk
			await using (var stream = new FileStream(savePath, FileMode.Create))
				await trackFile.CopyToAsync(stream);

			// 3) If you need to parse it immediately:
			if (ext == ".gpx")
			{
				//   ParseGpx(savePath);
				Console.WriteLine("gpx created");
			}
			else // ext == ".tcx"
			{
				//   ParseTcx(savePath);
				Console.WriteLine("tcx created");
			}

			// 4) Return whatever metadata/URL you need
			return Ok(new
			{
				fileName,
				relativePath = $"/{_uploadFolder}/{fileName}"
			});
		}
	}
}
