using APUS.Server.Domain.DTOs.Feature.Search;

namespace APUS.Server.Services.Implementations.UserServices
{
	public interface IFriendService
	{
		Task<bool> AcceptAsync(string me, string from, CancellationToken ct = default);
		Task<bool> CancelAsync(string me, string to, CancellationToken ct = default);
		Task<IReadOnlyList<FriendRequestItemDto>> GetIncomingAsync(string me, CancellationToken ct = default);
		Task<Dictionary<string, FriendStatusDto>> GetStatusesAsync(string me, IEnumerable<string> targets, CancellationToken ct = default);
		Task<bool> RejectAsync(string me, string from, CancellationToken ct = default);
		Task<bool> SendRequestAsync(string me, string to, CancellationToken ct = default);
	}
}