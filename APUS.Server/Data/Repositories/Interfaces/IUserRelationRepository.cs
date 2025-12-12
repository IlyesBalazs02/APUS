using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;

namespace APUS.Server.Data.Repositories.Interfaces
{
	public interface IUserRelationRepository
	{
		Task AddAsync(UserRelation relation, CancellationToken ct = default);
		Task<UserRelation?> FindAsync(string userId, string friendId, CancellationToken ct = default);
		Task<UserRelation?> FindEitherDirectionAsync(string a, string b, CancellationToken ct = default);
		Task<bool?> GetAllowFollowAsync(string userId, CancellationToken ct = default);
		Task<Dictionary<string, bool?>> GetAllowFollowMapAsync(IEnumerable<string> userIds, CancellationToken ct = default);
		Task<List<UserRelation>> GetBetweenAsync(string me, IEnumerable<string> targetIds, CancellationToken ct = default);
		Task<PagedResponse<UserSearchDto>> GetFriendsPagedAsync(string me, string? query, int skip, int take, CancellationToken ct = default);
		Task<int> GetIncomingCountAsync(string me, CancellationToken ct = default);
		Task<IReadOnlyList<(string FromUserId, string FromFullName)>> GetIncomingAsync(string me, CancellationToken ct = default);
		void Remove(UserRelation relation);
		Task SaveAsync(CancellationToken ct = default);
	}
}