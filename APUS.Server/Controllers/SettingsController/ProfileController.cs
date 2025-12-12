using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OsmSharp.API;
using Microsoft.AspNetCore.Authorization;
using APUS.Server.Services.Implementations.UserServices;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Controllers.SettingsController
{

	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class ProfileController : ControllerBase
	{
		private readonly UserManager<SiteUser> _userManager;
		private readonly IProfilePictureService _profilePictureService;

		public ProfileController(UserManager<SiteUser> userManager,
							 IProfilePictureService profilePictureService)
		{
			_userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
			_profilePictureService = profilePictureService ?? throw new ArgumentNullException(nameof(profilePictureService));
		}

		[HttpGet("get-profile")]
		public async Task<IActionResult> GetProfile()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return Unauthorized("User not found.");

			var relative = await _profilePictureService.GetProfilePictureUrlAsync(user.Id);
			var absolute = $"{Request.Scheme}://{Request.Host}{relative}";

			return Ok(new
			{
				firstName = user.FirstName,
				lastName = user.LastName,
				bio = user.Bio,
				avatarUrl = absolute
			});
		}

		public class UpdateProfileRequest
		{
			public string FirstName { get; set; } = string.Empty;
			public string LastName { get; set; } = string.Empty;
			public string Bio { get; set; } = string.Empty;
		}

		[HttpPost("update-profile")]
		public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized("User not found.");

			if (request.Bio.Length > 300)
				return BadRequest("Bio cannot exceed 300 characters.");

			user.FirstName = request.FirstName;
			user.LastName = request.LastName;
			user.Bio = request.Bio;

			var result = await _userManager.UpdateAsync(user);
			if (!result.Succeeded)
				return BadRequest("Failed to update profile.");

			return Ok(new { message = "Profile updated successfully." });
		}

		[HttpPost("upload-avatar")]
		[RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
		public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
		{
			if (file == null || file.Length == 0) return BadRequest("No file.");

			var user = await _userManager.GetUserAsync(User);
			if (user == null) return Unauthorized("User not found.");

			try
			{
				var url = await _profilePictureService.UploadProfilePictureAsync(user.Id, file);
				return Ok(new { url });
			}
			catch (InvalidOperationException ex)
			{
				return BadRequest(ex.Message);
			}
		}

		[HttpDelete("delete-avatar")]
		public async Task<IActionResult> DeleteAvatar()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return Unauthorized("User not found.");

			await _profilePictureService.DeleteProfilePictureAsync(user.Id);
			return Ok(new { message = "Avatar deleted." });
		}


	}
}
