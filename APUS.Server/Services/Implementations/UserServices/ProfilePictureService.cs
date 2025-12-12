using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Azure.Core;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics;

namespace APUS.Server.Services.Implementations.UserServices
{
	public class ProfilePictureService : IProfilePictureService
	{
		private readonly UserManager<SiteUser> _userMgr;
		private readonly string _webRootPath;

		private const string DefaultAvatarUrl = "/Perm/DefaultProfile.png";


		public ProfilePictureService(UserManager<SiteUser> userMgr, IWebHostEnvironment env)
		{
			_userMgr = userMgr ?? throw new ArgumentNullException(nameof(userMgr));
			if (env == null) throw new ArgumentNullException(nameof(env));
			_webRootPath = env.WebRootPath ?? throw new ArgumentNullException(nameof(env.WebRootPath));

		}

		public async Task<string> GetProfilePictureUrlAsync(string userId)
		{
			var user = await _userMgr.FindByIdAsync(userId);
			if (user == null) throw new InvalidOperationException("User not found.");

			var (physical, web) = GetMostRecentAvatar(userId);
			return physical is null ? DefaultAvatarUrl : web!;
		}

		public async Task<string> UploadProfilePictureAsync(string userId, IFormFile file)
		{
			if (file == null || file.Length == 0)
				throw new ArgumentException("No file provided.", nameof(file));

			var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowed.Contains(ext))
				throw new InvalidOperationException("Invalid file type.");

			var user = await _userMgr.FindByIdAsync(userId)
				?? throw new InvalidOperationException("User not found.");

			var avatarDir = Path.Combine(_webRootPath, "Users", userId, "Avatar");

			foreach (var old in Directory.GetFiles(avatarDir))
				System.IO.File.Delete(old);

			var fileName = $"avatar{ext}";
			var destinationPhysical = Path.Combine(avatarDir, fileName);

			await WriteFileAsync(file, destinationPhysical);

			var web = $"/Users/{userId}/Avatar/{fileName}".Replace('\\', '/');

			user.AvatarUrl = web;
			var updateRes = await _userMgr.UpdateAsync(user);
			if (!updateRes.Succeeded)
				throw new InvalidOperationException("Failed to reset user avatar URL.");

			return web;
		}

		public async Task DeleteProfilePictureAsync(string userId)
		{
			var user = await _userMgr.FindByIdAsync(userId)
				?? throw new InvalidOperationException("User not found.");

			var avatarDir = Path.Combine(_webRootPath, "Users", userId, "Avatar");
			if (Directory.Exists(avatarDir))
			{
				foreach (var f in Directory.GetFiles(avatarDir))
					System.IO.File.Delete(f);
			}

			user.AvatarUrl = "/Perm/DefaultProfile.png";
			var updateRes = await _userMgr.UpdateAsync(user);
			if (!updateRes.Succeeded)
				throw new InvalidOperationException("Failed to reset user avatar URL.");
		}

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

		private (string? physical, string? web) GetMostRecentAvatar(string userId)
		{
			var avatarDir = Path.Combine(_webRootPath, "Users", userId, "Avatar");
			if (!Directory.Exists(avatarDir)) return (null, null);

			var files = Directory.GetFiles(avatarDir);
			if (files.Length == 0) return (null, null);

			var newest = files
				.Select(p => new FileInfo(p))
				.OrderByDescending(fi => fi.LastWriteTimeUtc)
				.First();

			var relative = newest.FullName
				.Replace(_webRootPath, string.Empty)
				.Replace(Path.DirectorySeparatorChar, '/');

			var web = relative.StartsWith("/") ? relative : "/" + relative;
			return (newest.FullName, web);
		}
	}
}
