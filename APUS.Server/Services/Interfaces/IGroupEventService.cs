using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.DTOs.Groups;

namespace APUS.Server.Services.Interfaces
{
	public interface IGroupEventService
	{
		Task<GroupEventDto> CreateEventAsync(string userId, long groupId, CreateGroupEventRequest request, CancellationToken ct);
		Task DeleteEventAsync(string userId, long eventId, CancellationToken ct);
		Task<PagedResponse<GroupEventDto>> GetEventsPagedAsync(string userId, long groupId, int skip, int take, CancellationToken ct);
		Task<IReadOnlyList<GroupEventParticipantDto>> GetParticipantsAsync(string userId, long eventId, CancellationToken ct);
		Task JoinEventAsync(string userId, long groupId, long eventId, CancellationToken ct);
		Task LeaveEventAsync(string userId, long groupId, long eventId, CancellationToken ct);
	}
}