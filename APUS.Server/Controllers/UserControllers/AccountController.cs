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
			user.UserName = request.NewEmail; // optional, if username = email
			user.NormalizedEmail = request.NewEmail.ToUpper();
			user.NormalizedUserName = request.NewEmail.ToUpper();

			var result = await _userManager.UpdateAsync(user);
			if (!result.Succeeded)
				return BadRequest("Failed to update email.");

			return Ok(new { message = "Email updated successfully." });
		}
	}
}
