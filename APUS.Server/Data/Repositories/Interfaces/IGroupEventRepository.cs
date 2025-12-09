using APUS.Server.Domain.Entities.Groups;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IGroupEventRepository
	{
		Task<GroupEvent> AddAsync(GroupEvent entity, CancellationToken ct);
		Task<bool> AddParticipantAsync(long eventId, string userId, CancellationToken ct);
		Task DeleteAsync(GroupEvent entity, CancellationToken ct);
		Task<List<GroupEvent>> GetByGroupIdPagedAsync(long groupId, int skip, int take, CancellationToken ct);
		Task<GroupEvent?> GetByIdAsync(long eventId, CancellationToken ct);
		Task<IReadOnlyList<GroupEventParticipant>> GetParticipantsAsync(long eventId, CancellationToken ct);
		Task<bool> RemoveParticipantAsync(long eventId, string userId, CancellationToken ct);
	}
}