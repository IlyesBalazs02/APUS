using APUS.Server.Core.Helpers;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.GroupsController
{
	[ApiController]
	[Route("api/groups")]
	[Authorize]
	public class GroupSettingsController : ControllerBase
	{
		private readonly IGroupService _svc;

		public GroupSettingsController(IGroupService svc)
		{
			_svc = svc;
		}

		// Returns all editable group settings for admins.
		[HttpGet("{groupId:long}/settings")]
		public async Task<ActionResult<GroupSettingsDto>> GetSettings([FromRoute] long groupId, CancellationToken ct)
		{
			var userId = User.GetUserId();
			var settings = await _svc.GetSettingsAsync(userId, groupId, ct);
			return Ok(settings);
		}

		// Updates group settings such as name, bio, and permissions.
		[HttpPatch("{groupId:long}/settings")]
		public async Task<IActionResult> UpdateSettings([FromRoute] long groupId, [FromBody] UpdateGroupSettingsDto dto, CancellationToken ct)
		{
			var userId = User.GetUserId();
			await _svc.UpdateSettingsAsync(userId, groupId, dto, ct);
			return NoContent();
		}
	}
}
