using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace APUS.Server.Controllers.UserControllers
{
	[ApiController]
	[Route("api/[controller]")]
	[Authorize]
	public class FriendsController : ControllerBase
	{
		private readonly IFriendService _svc;
		public FriendsController(IFriendService svc) => _svc = svc;

		private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

		// TODO delete friends
		[HttpPost("status")]
		public async Task<ActionResult<Dictionary<string, FriendStatusDto>>> GetStatuses([FromBody] string[] userIds, CancellationToken ct)
		{
			var map = await _svc.GetStatusesAsync(CurrentUserId, userIds, ct);
			return Ok(map);
		}

		[HttpPost("request/{toUserId}")]
		public async Task<IActionResult> SendRequest([FromRoute] string toUserId, CancellationToken ct)
		{
			var ok = await _svc.SendRequestAsync(CurrentUserId, toUserId, ct);
			return ok ? Ok() : BadRequest();
		}

		[HttpGet("requests")]
		public async Task<ActionResult<IReadOnlyList<FriendRequestItemDto>>> Incoming(CancellationToken ct)
		{
			var list = await _svc.GetIncomingAsync(CurrentUserId, ct);
			return Ok(list);
		}

		[HttpPost("requests/{fromUserId}/accept")]
		public async Task<IActionResult> Accept([FromRoute] string fromUserId, CancellationToken ct)
		{
			var ok = await _svc.AcceptAsync(CurrentUserId, fromUserId, ct);
			return ok ? Ok() : BadRequest();
		}

		[HttpPost("requests/{fromUserId}/reject")]
		public async Task<IActionResult> Reject([FromRoute] string fromUserId, CancellationToken ct)
		{
			var ok = await _svc.RejectAsync(CurrentUserId, fromUserId, ct);
			return ok ? Ok() : BadRequest();
		}

		[HttpGet("requests/count")]
		public async Task<ActionResult<int>> IncomingCount(CancellationToken ct)
		{
			var n = await _svc.GetIncomingCountAsync(CurrentUserId, ct);
			return Ok(n);
		}

		[HttpGet("list")]
		[ProducesResponseType(typeof(PagedResponse<UserSearchDto>), StatusCodes.Status200OK)]
		public async Task<IActionResult> GetFriendsPaged(
		[FromQuery] string? query,
		[FromQuery] int skip = 0,
		[FromQuery] int take = 30,
		CancellationToken ct = default)
		{
			if (take is < 1 or > 100)
				take = 30;

			var result = await _svc.GetFriendsPagedAsync(CurrentUserId, query, skip, take, ct);
			return Ok(result);
		}

	}
}
