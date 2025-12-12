using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using APUS.Server.Data;

namespace APUS.Server.Controllers.SettingsController
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class PrivacyController : ControllerBase
	{
		private readonly UserManager<SiteUser> _userManager;
		private readonly AppDbContext _db;

		public PrivacyController(UserManager<SiteUser> userManager, AppDbContext db)
		{
			_userManager = userManager;
			_db = db;
		}
		public class PrivacyDto
		{
			public bool AllowFollow { get; set; }
			public string ActivityVisibility { get; set; } // "Everyone" | "Only followers" | "Only me"
			public string ProfileVisibility { get; set; }
		}

		private static VisibilityLevel ParseVisibility(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return VisibilityLevel.Everyone;

			var normalized = input.Trim().ToLowerInvariant();
			return normalized switch
			{
				"everyone" => VisibilityLevel.Everyone,
				"only followers" => VisibilityLevel.Followers,
				"followers" => VisibilityLevel.Followers,
				"only me" => VisibilityLevel.OnlyMe,
				"onlyme" => VisibilityLevel.OnlyMe,
				_ when Enum.TryParse<VisibilityLevel>(input, true, out var e) => e,
				_ => VisibilityLevel.Everyone
			};
		}

		private static PrivacyDto ToDto(PrivacySettings p) => new PrivacyDto
		{
			AllowFollow = p.AllowFollow,
			ActivityVisibility = p.ActivityVisibility.ToString(),
			ProfileVisibility = p.ProfileVisibility.ToString()
		};

		// Get current user's privacy settings
		[HttpGet]
		public async Task<ActionResult<PrivacyDto>> GetMine()
		{
			var user = await _userManager.GetUserAsync(User);
			if (user == null) return Unauthorized();

			var settings = await _db.PrivacySettings
				.AsNoTracking()
				.FirstOrDefaultAsync(p => p.UserId == user.Id);

			if (settings == null)
			{
				settings = new PrivacySettings
				{
					UserId = user.Id,
					AllowFollow = true,
					ActivityVisibility = VisibilityLevel.Everyone,
					ProfileVisibility = VisibilityLevel.Everyone
				};
				_db.PrivacySettings.Add(settings);
				await _db.SaveChangesAsync();
			}

			return Ok(ToDto(settings));
		}

		// Update current user's privacy settings.
		[HttpPut]
		public async Task<ActionResult<PrivacyDto>> UpdateMine([FromBody] PrivacyDto dto)
		{
			if (dto == null) return BadRequest("Body required.");

			var user = await _userManager.GetUserAsync(User);
			if (user == null) return Unauthorized();

			var settings = await _db.PrivacySettings
				.FirstOrDefaultAsync(p => p.UserId == user.Id);

			if (settings == null)
			{
				settings = new PrivacySettings { UserId = user.Id };
				_db.PrivacySettings.Add(settings);
			}

			settings.AllowFollow = dto.AllowFollow;
			settings.ActivityVisibility = ParseVisibility(dto.ActivityVisibility);
			settings.ProfileVisibility = ParseVisibility(dto.ProfileVisibility);
			settings.UpdatedAtUtc = DateTime.UtcNow;

			await _db.SaveChangesAsync();

			return Ok(ToDto(settings));
		}
	}
}
