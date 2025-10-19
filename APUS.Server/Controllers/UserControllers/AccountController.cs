using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.UserControllers
{

	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class AccountController : ControllerBase
	{
		private readonly UserManager<SiteUser> _userManager;

		public AccountController(UserManager<SiteUser> userManager)
		{
			_userManager = userManager;
		}

		public class ChangeEmailRequest
		{
			public string Password { get; set; }
			public string NewEmail { get; set; }
		}

		[HttpPost("change-email")]
		public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.NewEmail))
				return BadRequest("Missing required fields.");

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized("User not found.");

			// verify password
			var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
			if (!isPasswordValid)
				return BadRequest("Invalid password.");

			// update email
			user.Email = request.NewEmail;
			user.NormalizedEmail = request.NewEmail.ToUpper();

			var result = await _userManager.UpdateAsync(user);
			if (!result.Succeeded)
				return BadRequest("Failed to update email.");

			return Ok(new { message = "Email updated successfully." });
		}

		public class ChangePasswordRequest
		{
			public string currentPassword { get; set; } = string.Empty;
			public string newPassword { get; set; } = string.Empty;
		}

		[HttpPost("change-password")]
		public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.currentPassword) || string.IsNullOrWhiteSpace(request.newPassword))
				return BadRequest("Both current and new passwords are required.");

			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized("User not found.");

			var result = await _userManager.ChangePasswordAsync(user, request.currentPassword, request.newPassword);

			if (!result.Succeeded)
				return BadRequest(result.Errors.FirstOrDefault()?.Description ?? "Failed to change password.");

			return Ok(new { message = "Password updated successfully." });
		}

		public class GenderRequest
		{
			public string SelectedGender { get; set; } = string.Empty;
		}

		[HttpPost("change-gender")]
		public async Task<IActionResult> ChangeGender([FromBody] GenderRequest request)
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null)
				return Unauthorized("User not found.");

			if (!Enum.TryParse<GenderType>(request.SelectedGender, true, out var parsedGender))
				return BadRequest("Invalid gender value.");

			user.Gender = parsedGender;
			await _userManager.UpdateAsync(user);

			return Ok(new { message = "Gender updated successfully." });
		}

	}
}
