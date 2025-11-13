using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OsmSharp.API;
using APUS.Server.Core.Helpers;
using System.Diagnostics;

namespace APUS.Server.Controllers.GroupsController
{
	[ApiController]
	[Route("api/groups")]
	[Authorize]
	public class GroupsController : ControllerBase
	{
		private readonly IGroupService _svc;
		public GroupsController(IGroupService svc) => _svc = svc;

		[HttpPost]
		public async Task<ActionResult<GroupDto>> Create([FromBody] CreateGroupDto dto, CancellationToken ct)
		{
			var userId = User.GetUserId();
			var group = await _svc.CreateAsync(userId, dto, ct);
			return CreatedAtAction(nameof(Get), new { id = group.Id }, group);
		}

		[HttpGet("{id:long}")]
		public async Task<ActionResult<GroupDto>> Get([FromRoute] long id, CancellationToken ct)
		{
			var viewerId = User.GetUserId();
			var g = await _svc.GetForUserAsync(id, viewerId, ct);
			return g is null ? NotFound() : Ok(g);
		}


			[HttpGet]
		public async Task<ActionResult<List<GroupDto>>> Search([FromQuery] string? q, [FromQuery] int skip = 0, [FromQuery] int take = 20, CancellationToken ct = default)
		{
			take = Math.Clamp(take, 1, 50);
			var list = await _svc.SearchAsync(q, skip, take, ct);
			return Ok(list);
		}

		[HttpPost("{groupId:long}/join")]
		public async Task<IActionResult> Join([FromRoute] long groupId, CancellationToken ct)
		{
			await _svc.RequestToJoinAsync(User.GetUserId(), groupId, ct);
			return NoContent();
		}

		[HttpPost("requests/{requestId:long}/decide")]
		public async Task<IActionResult> Decide([FromRoute] long requestId, [FromBody] DecideJoinRequestDto dto, CancellationToken ct)
		{
			await _svc.ApproveOrRejectAsync(User.GetUserId(), requestId, dto.Approve, ct);
			return NoContent();
		}

		[HttpPost("{groupId:long}/leave")]
		public async Task<IActionResult> Leave([FromRoute] long groupId, CancellationToken ct)
		{
			await _svc.LeaveAsync(User.GetUserId(), groupId, ct);
			return NoContent();
		}

		[HttpPatch("{groupId:long}")]
		public async Task<IActionResult> Update([FromRoute] long groupId, [FromBody] UpdateGroupDto dto, CancellationToken ct)
		{
			await _svc.UpdateAsync(User.GetUserId(), groupId, dto, ct);
			return NoContent();
		}

		[HttpGet("{groupId:long}/members")]
		public async Task<ActionResult<List<GroupMemberDto>>> Members([FromRoute] long groupId, CancellationToken ct)
		{
			var members = await _svc.GetMembersAsync(groupId, ct);
			return Ok(members);
		}

		[HttpDelete("{groupId:long}/members/{userId}")]
		public async Task<IActionResult> Kick([FromRoute] long groupId, [FromRoute] string userId, CancellationToken ct)
		{
			await _svc.KickAsync(User.GetUserId(), groupId, userId, ct);
			return NoContent();
		}

		[HttpGet("{groupId:long}/requests")]
		public async Task<ActionResult<List<GroupJoinRequestDto>>> Requests(long groupId, CancellationToken ct)
		{
			var adminId = User.GetUserId();
			var reqs = await _svc.GetPendingRequestsAsync(adminId, groupId, ct);

			return Ok(reqs);
		}

	}
}
