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
	public sealed class GroupEventController : ControllerBase
	{
		private readonly IGroupEventService _svc;

		public GroupEventController(IGroupEventService svc)
		{
			_svc = svc;
		}

		// List events of a group
		[HttpGet("{groupId:long}/events")]
		[ProducesResponseType(typeof(PagedResponse<GroupEventDto>), StatusCodes.Status200OK)]
		public async Task<ActionResult<PagedResponse<GroupEventDto>>> List(long groupId, [FromQuery] int skip = 0, [FromQuery] int take = 10, CancellationToken ct = default)
		{
			var userId = User.GetUserId();
			var result = await _svc.GetEventsPagedAsync(userId, groupId, skip, take, ct);
			return Ok(result);
		}

		// Create a new event in a group
		[HttpPost("{groupId:long}/events")]
		[ProducesResponseType(typeof(GroupEventDto), StatusCodes.Status201Created)]
		public async Task<ActionResult<GroupEventDto>> Create(long groupId, [FromBody] CreateGroupEventRequest request, CancellationToken ct = default)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			var userId = User.GetUserId();
			var dto = await _svc.CreateEventAsync(userId, groupId, request, ct);

			return CreatedAtAction(
				nameof(List),
				new { groupId, skip = 0, take = 1 },
				dto);
		}

		// Delete an event
		[HttpDelete("events/{eventId:long}")]
		[ProducesResponseType(StatusCodes.Status204NoContent)]
		public async Task<IActionResult> Delete(long eventId, CancellationToken ct = default)
		{
			var userId = User.GetUserId();
			await _svc.DeleteEventAsync(userId, eventId, ct);
			return NoContent();
		}

		// Get participants for an event.
		[HttpGet("events/{eventId:long}/participants")]
		public async Task<ActionResult<IReadOnlyList<GroupEventParticipantDto>>> GetParticipants(long eventId, CancellationToken ct = default)
		{
			var userId = User.GetUserId();
			var result = await _svc.GetParticipantsAsync(userId, eventId, ct);
			return Ok(result);
		}

		// Join an event (current user)
		[HttpPost("{groupId:long}/events/{eventId:long}/participants")]
		public async Task<IActionResult> JoinEvent(long groupId, long eventId, CancellationToken ct = default)
		{
			var userId = User.GetUserId();
			await _svc.JoinEventAsync(userId, groupId, eventId, ct);
			return NoContent();
		}

		// Leave an event (current user)
		[HttpDelete("{groupId:long}/events/{eventId:long}/participants")]
		public async Task<IActionResult> LeaveEvent(long groupId, long eventId, CancellationToken ct = default)
		{
			var userId = User.GetUserId();
			await _svc.LeaveEventAsync(userId, groupId, eventId, ct);
			return NoContent();
		}
	}
}
