using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IGroupRepository
	{
		Task<GroupJoinRequest> AddJoinRequestAsync(long groupId, string userId, DateTime nowUtc, CancellationToken ct);
		Task AddMemberAsync(long groupId, string userId, GroupRole role, DateTime joinedAtUtc, CancellationToken ct);
		Task<int> AdminCountAsync(long groupId, string exceptUserId, CancellationToken ct);
		Task<Group> CreateAsync(Group g, CancellationToken ct);
		Task<Group?> GetAsync(long id, CancellationToken ct);
		Task<GroupJoinRequest?> GetJoinRequestWithGroupAsync(long requestId, CancellationToken ct);
		Task<bool> HasPendingRequestAsync(long groupId, string userId, CancellationToken ct);
		Task<bool> IsMemberAsync(long groupId, string userId, CancellationToken ct);
		IQueryable<GroupMembership> MembersQuery(long groupId);
		IQueryable<GroupJoinRequest> PendingRequestsQuery(long groupId);
		Task RemoveMemberAsync(long groupId, string userId, CancellationToken ct);
		Task<List<GroupDto>> SearchAsync(string? q, int skip, int take, CancellationToken ct);
		Task UpdateAsync(Group g, CancellationToken ct);
		IQueryable<GroupJoinRequest> JoinRequestsQuery(long groupId);
		Task<GroupJoinRequest?> GetJoinRequestAsync(long groupId, string userId, CancellationToken ct);
		Task UpdateJoinRequestAsync(GroupJoinRequest request, CancellationToken ct);
		IQueryable<GroupPost> PostsQuery(long groupId);
		Task AddPostAsync(GroupPost post, CancellationToken ct);
		Task<GroupPost?> GetPostWithGroupAsync(long postId, CancellationToken ct);
		Task DeletePostAsync(GroupPost post, CancellationToken ct);

	}
}