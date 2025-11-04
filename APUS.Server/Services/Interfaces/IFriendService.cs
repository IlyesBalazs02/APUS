using APUS.Server.Domain.DTOs.Feature.Search;

namespace APUS.Server.Services.Interfaces
{
	public interface IFriendService
	{
		Task<bool> AcceptAsync(string me, string from, CancellationToken ct = default);
		Task<PagedResponse<UserSearchDto>> GetFriendsPagedAsync(string me, string? query, int skip, int take, CancellationToken ct = default);
		Task<IReadOnlyList<FriendRequestItemDto>> GetIncomingAsync(string me, CancellationToken ct = default);
		Task<int> GetIncomingCountAsync(string me, CancellationToken ct = default);
		Task<Dictionary<string, FriendStatusDto>> GetStatusesAsync(string me, IEnumerable<string> targets, CancellationToken ct = default);
		Task<bool> RejectAsync(string me, string from, CancellationToken ct = default);
		Task<bool> SendRequestAsync(string me, string to, CancellationToken ct = default);
	}
}