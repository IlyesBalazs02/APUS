using APUS.Server.DTOs;
using APUS.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class AuthController : ControllerBase
	{
		private readonly IConfiguration _config;
		private readonly UserManager<SiteUser> _userMgr;
		private readonly SignInManager<SiteUser> _signInMgr;

		public AuthController(IConfiguration config, UserManager<SiteUser> userMgr, SignInManager<SiteUser> signInMgr)
		{
			_config = config;
			_userMgr = userMgr;
			_signInMgr = signInMgr;
		}

		[HttpPost("register")]
		[AllowAnonymous]
		public async Task<IActionResult> Register(RegisterDto dto)
		{
			if (dto.Password != dto.ConfirmPassword)
				return BadRequest("Passwords must match.");

			var user = new SiteUser { UserName = dto.Email, Email = dto.Email };
			var result = await _userMgr.CreateAsync(user, dto.Password);

			if (!result.Succeeded)
				return BadRequest(result.Errors);

			// Optionally sign in or return token...
			return Ok();
		}

		[HttpPost("login")]
		[AllowAnonymous]
		public async Task<IActionResult> Login(LoginDto dto)
		{
			var result = await _signInMgr.PasswordSignInAsync(
				dto.Email, dto.Password, isPersistent: false, lockoutOnFailure: false);

			if (!result.Succeeded)
				return Unauthorized("Invalid login");

			var token = GenerateJwtToken(dto.Email);
			return Ok(new { token });
		}

		private string GenerateJwtToken(string email)
		{
			var jwtKey = _config["Jwt:Key"];
			var jwtIssuer = _config["Jwt:Issuer"];
			var jwtAudience = _config["Jwt:Audience"];

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var claims = new[]
			{
		new Claim(JwtRegisteredClaimNames.Sub, email),
		new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
    };

			var token = new JwtSecurityToken(
				issuer: jwtIssuer,
				audience: jwtAudience,
				claims: claims,
				expires: DateTime.UtcNow.AddHours(2),   
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
