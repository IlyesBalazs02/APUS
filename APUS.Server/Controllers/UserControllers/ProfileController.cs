using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.User;
using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OsmSharp.API;
using System.Security.Claims;

namespace APUS.Server.Controllers.UserControllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ProfileController : ControllerBase
	{
		private readonly ILogger<ActivitiesController> _logger;
		private readonly IActivityRepository _activityRepository;
		private readonly UserManager<SiteUser> _userMgr;

		public ProfileController(ILogger<ActivitiesController> logger, IActivityRepository activityRepository, UserManager<SiteUser> userMgr)
		{
			_logger = logger;
			_activityRepository = activityRepository;
			_userMgr = userMgr;
		}

		[HttpGet("me")]
		[Authorize]
		public async Task<ActionResult<ProfileDto>> GetMyProfile()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

			var user = await _userMgr.FindByIdAsync(userId);

			if (user == null)
				return NotFound($"User doesnt exist with id:{userId}");

			//ONLY TEMPORARY
			var name = $"{user.FirstName} {user.LastName}";

			return Ok(new ProfileDto { Name = name });
		}

		[HttpGet("{id}", Name = "GetUserProfile")]
		[Authorize]
		public async Task<ActionResult<ProfileDto>> GetUserProfile(string id)
		{
			var userId = id;

			var user = await _userMgr.FindByIdAsync(userId);

			if (user == null)
				return NotFound();

			var name = $"{user.FirstName} {user.LastName}";

			return Ok(new ProfileDto { Name = name });
		}

	}
}
