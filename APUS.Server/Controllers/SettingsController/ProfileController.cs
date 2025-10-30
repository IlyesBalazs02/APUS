using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OsmSharp.API;
using Microsoft.AspNetCore.Authorization;

namespace APUS.Server.Controllers.SettingsController
{

	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class ProfileController : ControllerBase
	{
		private readonly UserManager<SiteUser> _userManager;

		public ProfileController(UserManager<SiteUser> userManager)
		{
			_userManager = userManager;
		}

		[HttpGet("get-profile")]
		public async Task<IActionResult> GetProfile()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized("User not found.");

			return Ok(new
			{
				firstName = user.FirstName,
				lastName = user.LastName,
				bio = user.Bio
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

			await _userManager.UpdateAsync(user);

			return Ok(new { message = "Profile updated successfully." });
		}

	}
}
