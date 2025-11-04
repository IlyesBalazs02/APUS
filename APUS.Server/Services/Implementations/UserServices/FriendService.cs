using APUS.Server.Data;
using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Services.Implementations.UserServices
{
	public class FriendService : IFriendService
	{
		private readonly IUserRelationRepository _repo;
		public FriendService(IUserRelationRepository repo) => _repo = repo;

		public async Task<Dictionary<string, FriendStatusDto>> GetStatusesAsync(string me, IEnumerable<string> targets, CancellationToken ct = default)
		{
			var ids = targets.Where(id => id != me).Distinct().ToArray();
			if (ids.Length == 0) return new();

			var rels = await _repo.GetBetweenAsync(me, ids, ct);
			var allow = await _repo.GetAllowFollowMapAsync(ids, ct);

			var result = new Dictionary<string, FriendStatusDto>(ids.Length);

			foreach (var id in ids)
			{
				var rel = rels.FirstOrDefault(r => (r.UserId == me && r.FriendId == id) || (r.UserId == id && r.FriendId == me));
				if (rel is null)
				{
					var can = allow.TryGetValue(id, out var ok) ? (ok ?? true) : true;
					result[id] = new FriendStatusDto(id, can, can ? null : "User does not allow follow", null, null);
					continue;
				}

				if (rel.Status == UserRelationStatus.Accepted)
				{
					result[id] = new FriendStatusDto(id, false, "Already friends", "Accepted", null);
				}
				else
				{
					bool outgoing = rel.UserId == me;
					result[id] = new FriendStatusDto(
						id, false,
						outgoing ? "Request already sent" : "Incoming request pending",
						"Pending",
						outgoing ? "Outgoing" : "Incoming"
					);
				}
			}

			return result;
		}

		public async Task<bool> SendRequestAsync(string me, string to, CancellationToken ct = default)
		{
			if (me == to) return false;

			var existing = await _repo.FindEitherDirectionAsync(me, to, ct);
			if (existing != null)
			{
				if (existing.Status == UserRelationStatus.Accepted) return false;

				if (existing.Status == UserRelationStatus.Pending && existing.UserId == to && existing.FriendId == me)
				{
					existing.Status = UserRelationStatus.Accepted;
					await _repo.SaveAsync(ct);
					return true;
				}
				return false;
			}

			var follow = await _repo.GetAllowFollowAsync(to, ct);
			if (follow.HasValue && !follow.Value) return false;

			await _repo.AddAsync(new UserRelation { UserId = me, FriendId = to, Status = UserRelationStatus.Pending }, ct);
			await _repo.SaveAsync(ct);
			return true;
		}

		public async Task<IReadOnlyList<FriendRequestItemDto>> GetIncomingAsync(string me, CancellationToken ct = default)
		{
			var list = await _repo.GetIncomingAsync(me, ct);
			return list.Select(x => new FriendRequestItemDto(x.FromUserId, x.FromFullName, null)).ToList();
		}

		public async Task<bool> AcceptAsync(string me, string from, CancellationToken ct = default)
		{
			var rel = await _repo.FindAsync(from, me, ct);
			if (rel is null || rel.Status != UserRelationStatus.Pending) return false;
			rel.Status = UserRelationStatus.Accepted;
			await _repo.SaveAsync(ct);
			return true;
		}

		public async Task<bool> RejectAsync(string me, string from, CancellationToken ct = default)
		{
			var rel = await _repo.FindAsync(from, me, ct);
			if (rel is null || rel.Status != UserRelationStatus.Pending) return false;
			_repo.Remove(rel);
			await _repo.SaveAsync(ct);
			return true;
		}

		public Task<int> GetIncomingCountAsync(string me, CancellationToken ct = default)
			=> _repo.GetIncomingCountAsync(me, ct);

		public Task<PagedResponse<UserSearchDto>> GetFriendsPagedAsync(string me, string? query, int skip, int take, CancellationToken ct = default)
			=> _repo.GetFriendsPagedAsync(me, query, skip, take, ct);
	}
}
