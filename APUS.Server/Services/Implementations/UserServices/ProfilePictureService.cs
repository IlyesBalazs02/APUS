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

			return string.IsNullOrWhiteSpace(user.AvatarUrl)
				? DefaultAvatarUrl
				: user.AvatarUrl;

		}

		public async Task<string> UploadProfilePictureAsync(string userId, IFormFile file)
		{
			if (file == null || file.Length == 0) throw new ArgumentException("No file provided.", nameof(file));

			var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
			var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
			if (!allowed.Contains(ext)) throw new InvalidOperationException("Invalid file type.");

			// Normalize a deterministic file name, 1 file per user
			var fileName = $"avatar{ext}";

			var userFolderPhysical = Path.Combine(_webRootPath, "Users", userId);

			// Clean old avatar file for this user
			foreach (var old in Directory.GetFiles(userFolderPhysical, "avatar.*"))
				System.IO.File.Delete(old);

			var destinationPhysical = Path.Combine(userFolderPhysical, fileName);


			await WriteFileAsync(file, destinationPhysical).ConfigureAwait(false);

			var user = await _userMgr.FindByIdAsync(userId)
			   ?? throw new InvalidOperationException("User not found.");

			var publicUrl = $"/Users/{userId}/{fileName}".Replace('\\', '/');
			user.AvatarUrl = publicUrl;

			var result = await _userMgr.UpdateAsync(user);
			if (!result.Succeeded)
				throw new InvalidOperationException("Failed to update user with avatar URL.");

			return publicUrl;
		}

		public async Task DeleteProfilePictureAsync(string userId)
		{
			var user = await _userMgr.FindByIdAsync(userId)
				?? throw new InvalidOperationException("User not found.");

			// Delete physical file(s)
			var userFolderPhysical = Path.Combine(_webRootPath, "Users", userId);
			if (Directory.Exists(userFolderPhysical))
			{
				foreach (var f in Directory.GetFiles(userFolderPhysical, "avatar.*"))
					System.IO.File.Delete(f);
			}

			// Reset to default
			user.AvatarUrl = null;
			await _userMgr.UpdateAsync(user);
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
	}
}
