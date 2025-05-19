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

			var token = await GenerateJwtTokenAsync(dto.Email);
			return Ok(new { token });
		}


		private async Task<string> GenerateJwtTokenAsync(string email)
		{
			// 1) Look up the full user so you can grab its Id
			var user = await _userMgr.FindByEmailAsync(email);

			// 2) Build your claims, including the NameIdentifier (user.Id) and email
			var claims = new List<Claim>
	{
		new Claim(ClaimTypes.NameIdentifier, user.Id),              // ← now the real ID
        new Claim(JwtRegisteredClaimNames.Email, user.Email),       // ← keep email if you like
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
	};

			// 3) Sign the token exactly as before
			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: _config["Jwt:Issuer"],
				audience: _config["Jwt:Audience"],
				claims: claims,
				expires: DateTime.UtcNow.AddHours(2),
				signingCredentials: creds
			);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}
}
