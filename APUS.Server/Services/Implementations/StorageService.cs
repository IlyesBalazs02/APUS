using APUS.Server.Services.Interfaces;
using Azure.Core;
using Microsoft.AspNetCore.Http.HttpResults;

namespace APUS.Server.Services.Implementations
{
	public class StorageService : IStorageService
	{
		private readonly string _uploadsRoot;

		private const string UsersFolder = "Users";
		private const string ActivitiesFolder = "Activities";
		private const string ImagesFolder = "Images";
		private const string TrackFolder = "Track";
		private const string ActivityImageFileName = "ActivityTrackImage.png";
		private static readonly string[] SupportedTrackExtensions = new[] { ".tcx", ".gpx" };

		public StorageService(IWebHostEnvironment env)
		{
			if (env == null) throw new ArgumentNullException(nameof(env));
			_uploadsRoot = Path.Combine(env.WebRootPath, UsersFolder);
		}

		public void CreateUserFolder(string userId)
		{
			var path = Path.Combine(_uploadsRoot, userId, "Activities");
			Directory.CreateDirectory(path);
		}

		public void CreateActivityFolder(string activityId, string userId)
		{
			var activityBase = GetActivityRootPath(userId, activityId);
			Directory.CreateDirectory(activityBase);
			Directory.CreateDirectory(Path.Combine(activityBase, ImagesFolder));
			Directory.CreateDirectory(Path.Combine(activityBase, TrackFolder));
		}

		public async Task SaveTrackAsync(string activityId, string userId, IFormFile trackFile)
		{
			if (trackFile?.Length > 0)
			{
				var target = Path.Combine(GetTrackPath(userId, activityId), Path.GetFileName(trackFile.FileName));
				await WriteFileAsync(trackFile, target).ConfigureAwait(false);
			}
		}


		public string ReturnFirstFilePath(string activityId, string userId)
		{
			var folder = GetTrackPath(userId, activityId);
			if (!Directory.Exists(folder)) return null;

			return Directory
				.EnumerateFiles(folder)
				.FirstOrDefault(file => SupportedTrackExtensions
					.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
				);
		}

		public string ReturnTrackImagePath(string activityId, string userId)
		{
			return Path.Combine(GetActivityRootPath(userId, activityId), ActivityImageFileName);
		}

		public async Task SaveImagesAsync(string activityId, IFormFileCollection images, string userId)
		{
			var folder = Path.Combine(GetActivityRootPath(userId, activityId), ImagesFolder);
			Directory.CreateDirectory(folder);

			foreach (var image in images ?? Enumerable.Empty<IFormFile>())
			{
				if (image.Length == 0) continue;
				var target = Path.Combine(folder, Path.GetFileName(image.FileName));
				await WriteFileAsync(image, target).ConfigureAwait(false);
			}
		}

		public IEnumerable<string> GetImageFileNames(string activityId, string userId)
		{
			var folder = Path.Combine(GetActivityRootPath(userId, activityId), ImagesFolder);
			if (!Directory.Exists(folder)) return Enumerable.Empty<string>();

			return Directory
				.GetFiles(folder)
				.Select(Path.GetFileName);
		}

		#region Helpers

		private string GetUserActivitiesPath(string userId)
			=> Path.Combine(_uploadsRoot, userId, ActivitiesFolder);

		private string GetActivityRootPath(string userId, string activityId)
			=> Path.Combine(_uploadsRoot, userId, ActivitiesFolder, activityId);

		private string GetTrackPath(string userId, string activityId)
			=> Path.Combine(GetActivityRootPath(userId, activityId), TrackFolder);

		private static async Task WriteFileAsync(IFormFile file, string destination)
		{
			await using var stream = new FileStream(
				destination,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				bufferSize: 81920,
				useAsync: true
			);

			await file.CopyToAsync(stream).ConfigureAwait(false);
		}

		#endregion

	}
}
