using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace APUS.Server.Services
{
	public class ActivityStorageService : IActivityStorageService
	{
		private readonly string _uploadsRoot;

		public ActivityStorageService(IWebHostEnvironment env)
			=> _uploadsRoot = Path.Combine(env.WebRootPath, "Activities");

		public void CreateActivityFolder(string activityId)
		{
			
			var path = Path.Combine(_uploadsRoot, activityId);
			Directory.CreateDirectory(path);
			path = Path.Combine(_uploadsRoot, activityId, "Images");
			Directory.CreateDirectory(path);
			path = Path.Combine(_uploadsRoot, activityId, "Tracks");
			Directory.CreateDirectory(path);
		}

		public async Task SaveImages(string activityId, IFormFileCollection images)
		{
			var imagesFolder = Path.Combine(_uploadsRoot, activityId, "Images");

			foreach (var image in images)
			{
				if (image.Length == 0) continue;

				var fileName = Path.GetFileName(image.FileName);
				var fullPath = Path.Combine(imagesFolder, fileName);

				await image.CopyToAsync(new FileStream(fullPath, FileMode.Create));
			}
		}

		public IEnumerable<string> GetImageFileNames(string activityId)
		{
			var folder = Path.Combine(_uploadsRoot, activityId, "Images");
			if (!Directory.Exists(folder))
				return Array.Empty<string>();

			return Directory
			  .GetFiles(folder)
			  .Select(Path.GetFileName);
		}

	}
}
