using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Services.Interfaces;
using OsmSharp.API;

namespace APUS.Server.Services.Implementations.GroupServices
{
	public sealed class GroupEventService : IGroupEventService
	{
		private readonly IGroupEventRepository _events;
		private readonly IGroupRepository _groups;

		public GroupEventService(
			IGroupEventRepository events,
			IGroupRepository groups)
		{
			_events = events;
			_groups = groups;
		}

		public async Task<PagedResponse<GroupEventDto>> GetEventsPagedAsync(string userId, long groupId, int skip, int take, CancellationToken ct)
		{
			if (take < 1 || take > 50) take = 10;

			var list = await _events.GetByGroupIdPagedAsync(groupId, skip, take + 1, ct);

			var hasMore = list.Count > take;
			if (hasMore)
				list.RemoveAt(list.Count - 1);

			var items = list.Select(e => MapToDto(e, userId)).ToList();

			return new PagedResponse<GroupEventDto>
			{
				Items = items,
				HasMore = hasMore
			};
		}

		public async Task<GroupEventDto> CreateEventAsync(string userId, long groupId, CreateGroupEventRequest request, CancellationToken ct)
		{
			var group = await _groups.GetAsync(groupId, ct)
				?? throw new InvalidOperationException("Group not found.");

			var now = DateTime.UtcNow;

			var entity = new GroupEvent
			{
				GroupId = groupId,
				Title = request.Title.Trim(),
				Description = string.IsNullOrWhiteSpace(request.Description)
					? null
					: request.Description.Trim(),
				TrackActivityId = string.IsNullOrWhiteSpace(request.TrackActivityId)
					? null
					: request.TrackActivityId,
				StartsAtUtc = request.StartsAtUtc,
				CreatedByUserId = userId,
				CreatedAtUtc = now
			};

			entity = await _events.AddAsync(entity, ct);

			return MapToDto(entity, userId);
		}

		public async Task DeleteEventAsync(string userId, long eventId, CancellationToken ct)
		{
			var entity = await _events.GetByIdAsync(eventId, ct)
				?? throw new InvalidOperationException("Event not found.");

			await _events.DeleteAsync(entity, ct);
		}

		public async Task JoinEventAsync(string userId, long groupId, long eventId, CancellationToken ct)
		{
			var ev = await _events.GetByIdAsync(eventId, ct)
					 ?? throw new InvalidOperationException("Event not found.");

			if (ev.GroupId != groupId)
				throw new InvalidOperationException("Event does not belong to this group.");

			await _events.AddParticipantAsync(eventId, userId, ct);
		}

		public async Task LeaveEventAsync(string userId, long groupId, long eventId, CancellationToken ct)
		{
			var ev = await _events.GetByIdAsync(eventId, ct)
					 ?? throw new InvalidOperationException("Event not found.");

			if (ev.GroupId != groupId)
				throw new InvalidOperationException("Event does not belong to this group.");

			await _events.RemoveParticipantAsync(eventId, userId, ct);
		}

		public async Task<IReadOnlyList<GroupEventParticipantDto>> GetParticipantsAsync(string userId, long eventId, CancellationToken ct)
		{
			var ev = await _events.GetByIdAsync(eventId, ct)
					 ?? throw new InvalidOperationException("Event not found.");

			var participants = await _events.GetParticipantsAsync(eventId, ct);

			return participants.Select(p => new GroupEventParticipantDto
			{
				UserId = p.UserId,
				FullName = $"{p.User.FirstName} {p.User.LastName}",
				AvatarUrl = p.User.AvatarUrl,
				JoinedAtUtc = p.JoinedAtUtc
			}).ToList();
		}

		private static GroupEventDto MapToDto(GroupEvent e, string userId)
		{
			return new GroupEventDto
			{
				Id = e.Id,
				GroupId = e.GroupId,
				Title = e.Title,
				Description = e.Description,
				TrackActivityId = e.TrackActivityId,
				CreatedByUserId = e.CreatedByUserId,
				CreatedByFullName = $"{e.CreatedByUser.FirstName} {e.CreatedByUser.LastName}",
				CreatedByAvatarUrl = e.CreatedByUser.AvatarUrl,
				CreatedAtUtc = e.CreatedAtUtc,
				StartsAtUtc = e.StartsAtUtc,
				ParticipantCount = e.Participants.Count,
				IsJoinedByCurrentUser = e.Participants.Any(p => p.UserId == userId)
			};
		}
	}
}
