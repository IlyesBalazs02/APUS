using APUS.Server.Core.Helpers;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.GroupsController
{
	[ApiController]
	[Route("api/groups")]
	[Authorize]
	public class GroupPostController : ControllerBase
	{
		private readonly IGroupService _svc;
		public GroupPostController(IGroupService svc) => _svc = svc;

		[HttpGet("{groupId:long}/posts")]
		public async Task<ActionResult<PagedResponse<GroupPostDto>>> List(
			long groupId,
			[FromQuery] int skip = 0,
			[FromQuery] int take = 20,
			CancellationToken ct = default)
		{
			take = Math.Clamp(take, 1, 50);
			var userId = User.GetUserId();
			var result = await _svc.GetPostsAsync(userId, groupId, skip, take, ct);
			return Ok(result);
		}

		[HttpPost("{groupId:long}/posts")]
		public async Task<ActionResult<GroupPostDto>> Create(
			long groupId,
			[FromBody] CreateGroupPostDto dto,
			CancellationToken ct)
		{
			var userId = User.GetUserId();
			var post = await _svc.CreatePostAsync(userId, groupId, dto, ct);
			return CreatedAtAction(nameof(List), new { groupId, skip = 0, take = 1 }, post);
		}

		[HttpDelete("posts/{postId:long}")]
		public async Task<IActionResult> Delete(long postId, CancellationToken ct)
		{
			var userId = User.GetUserId();
			await _svc.DeletePostAsync(userId, postId, ct);
			return NoContent();
		}
	}
}
