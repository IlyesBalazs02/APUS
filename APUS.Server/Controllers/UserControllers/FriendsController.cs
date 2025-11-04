using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Services.Implementations.UserServices;
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

		// POST api/friends/status
		[HttpPost("status")]
		public async Task<ActionResult<Dictionary<string, FriendStatusDto>>> GetStatuses([FromBody] string[] userIds, CancellationToken ct)
		{
			var map = await _svc.GetStatusesAsync(CurrentUserId, userIds, ct);
			return Ok(map);
		}

		// POST api/friends/request/{toUserId}
		[HttpPost("request/{toUserId}")]
		public async Task<IActionResult> SendRequest([FromRoute] string toUserId, CancellationToken ct)
		{
			var ok = await _svc.SendRequestAsync(CurrentUserId, toUserId, ct);
			return ok ? Ok() : BadRequest();
		}

		// GET api/friends/requests
		[HttpGet("requests")]
		public async Task<ActionResult<IReadOnlyList<FriendRequestItemDto>>> Incoming(CancellationToken ct)
		{
			var list = await _svc.GetIncomingAsync(CurrentUserId, ct);
			return Ok(list);
		}

		// POST api/friends/requests/{fromUserId}/accept
		[HttpPost("requests/{fromUserId}/accept")]
		public async Task<IActionResult> Accept([FromRoute] string fromUserId, CancellationToken ct)
		{
			var ok = await _svc.AcceptAsync(CurrentUserId, fromUserId, ct);
			return ok ? Ok() : BadRequest();
		}

		// POST api/friends/requests/{fromUserId}/reject
		[HttpPost("requests/{fromUserId}/reject")]
		public async Task<IActionResult> Reject([FromRoute] string fromUserId, CancellationToken ct)
		{
			var ok = await _svc.RejectAsync(CurrentUserId, fromUserId, ct);
			return ok ? Ok() : BadRequest();
		}

		// optional cancel: POST api/friends/requests/{toUserId}/cancel
		[HttpPost("requests/{toUserId}/cancel")]
		public async Task<IActionResult> Cancel([FromRoute] string toUserId, CancellationToken ct)
		{
			var ok = await _svc.CancelAsync(CurrentUserId, toUserId, ct);
			return ok ? Ok() : BadRequest();
		}
	}
}
