using APUS.Server.Domain.DTOs.Groups;

namespace APUS.Server.Services.Interfaces
{
	public interface IGroupService
	{
		Task ApproveOrRejectAsync(string adminId, long requestId, bool approve, CancellationToken ct);
		Task<GroupDto> CreateAsync(string creatorId, CreateGroupDto dto, CancellationToken ct);
		Task<GroupDto?> GetAsync(long id, CancellationToken ct);
		Task<GroupDto?> GetForUserAsync(long id, string viewerId, CancellationToken ct);
		Task<List<GroupMemberDto>> GetMembersAsync(long groupId, CancellationToken ct);
		Task KickAsync(string adminId, long groupId, string targetUserId, CancellationToken ct);
		Task LeaveAsync(string userId, long groupId, CancellationToken ct);
		Task RequestToJoinAsync(string userId, long groupId, CancellationToken ct);
		Task<List<GroupDto>> SearchAsync(string? q, int skip, int take, CancellationToken ct);
		Task UpdateAsync(string adminId, long groupId, UpdateGroupDto dto, CancellationToken ct);
		Task<List<GroupJoinRequestDto>> GetPendingRequestsAsync(string adminId, long groupId, CancellationToken ct);

	}
}