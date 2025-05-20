using APUS.Server.Services.Interfaces;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace APUS.Server.Services.Implementations
{
	public class StorageService : IStorageService
	{
		private readonly string _uploadsRoot;

		public StorageService(IWebHostEnvironment env)
			=> _uploadsRoot = Path.Combine(env.WebRootPath, "Users");

		public void CreateUserFolder(string userId)
		{
			var path = Path.Combine(_uploadsRoot, userId, "Activities");
			Directory.CreateDirectory(path);
		}

		public void CreateActivityFolder(string activityId, string userId)
		{
			var createActivityPath = Path.Combine(_uploadsRoot, userId, "Activities", activityId);

			Directory.CreateDirectory(createActivityPath);

			var path = Path.Combine(createActivityPath, "Images");
			Directory.CreateDirectory(path);

			path = Path.Combine(createActivityPath, "Track");
			Directory.CreateDirectory(path);
		}

		public async Task SaveTrack(string activityId, string userId, IFormFile trackFile)
		{
			if (trackFile == null || trackFile.Length == 0)
				return;

			var filename = Path.GetFileName(trackFile.FileName);
			var fullpath = Path.Combine(_uploadsRoot, userId,
										"Activities", activityId,
										"Track", filename);

			// ensure the FileStream is closed/disposed immediately
			await using var fs = new FileStream(
				fullpath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 81920,
				useAsync: true);

			await trackFile.CopyToAsync(fs);
		}

		public string ReturnFilePath(string activityId, string userId)
		{
			string[] extensions = new string[] { ".tcx", ".gpx" };
			string folderPath = Path.Combine(_uploadsRoot, userId, "Activities", activityId, "Track");


			return Directory.EnumerateFiles(folderPath)
						.FirstOrDefault(file =>
							extensions.Any(ext =>
								file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)
							)
						);
		}

		public string ReturnTrackPngPath(string activityId, string userId)
		{
			string folderPath = Path.Combine(_uploadsRoot, "Users" ,userId, "Activities", activityId, "ActivityTrackImage.png");

			return folderPath;
		}

		public async Task SaveImages(string activityId, IFormFileCollection images, string userId)
		{
			var imagesFolder = Path.Combine(_uploadsRoot, userId, "Activities", activityId, "Images");

			foreach (var image in images)
			{
				if (image.Length == 0) continue;

				var fileName = Path.GetFileName(image.FileName);
				var fullPath = Path.Combine(imagesFolder, fileName);

				await image.CopyToAsync(new FileStream(fullPath, FileMode.Create));
			}
		}

		public IEnumerable<string> GetImageFileNames(string activityId, string userId)
		{
			var folder = Path.Combine(_uploadsRoot, userId, "Activities", activityId, "Images");
			if (!Directory.Exists(folder))
				return Array.Empty<string>();

			return Directory
			  .GetFiles(folder)
			  .Select(Path.GetFileName);
		}

	}
}
