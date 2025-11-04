using APUS.Server.Data;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Services.Implementations.UserServices
{
	public class FriendService : IFriendService
	{
		private readonly AppDbContext _db;
		public FriendService(AppDbContext db) => _db = db;

		public async Task<Dictionary<string, FriendStatusDto>> GetStatusesAsync(string me, IEnumerable<string> targets, CancellationToken ct = default)
		{
			var ids = targets.Where(id => id != me).Distinct().ToArray();
			if (ids.Length == 0) return new();

			// pull existing relations either direction in one query
			var rels = await _db.UserRelations
				.Where(r => (r.UserId == me && ids.Contains(r.FriendId)) ||
							(r.FriendId == me && ids.Contains(r.UserId)))
				.Select(r => new { r.UserId, r.FriendId, r.Status })
				.ToListAsync(ct);

			var allowFollow = await _db.PrivacySettings
				.Where(p => ids.Contains(p.UserId))
				.Select(p => new { p.UserId, p.AllowFollow })
				.ToListAsync(ct);

			var allowMap = allowFollow.ToDictionary(x => x.UserId, x => x.AllowFollow);

			var result = new Dictionary<string, FriendStatusDto>(ids.Length);
			foreach (var id in ids)
			{
				var rel = rels.FirstOrDefault(r => (r.UserId == me && r.FriendId == id) || (r.UserId == id && r.FriendId == me));
				if (rel is null)
				{
					var can = allowMap.TryGetValue(id, out var ok) ? ok : true; // default true if no settings yet
					result[id] = new FriendStatusDto(id, can, can ? null : "User does not allow follow", null, null);
					continue;
				}

				if (rel.Status == UserRelationStatus.Accepted)
				{
					result[id] = new FriendStatusDto(id, false, "Already friends", "Accepted", null);
					continue;
				}

				// Pending either direction
				bool outgoing = (rel.UserId == me && rel.FriendId == id);
				result[id] = new FriendStatusDto(
					id,
					false,
					outgoing ? "Request already sent" : "Incoming request pending",
					"Pending",
					outgoing ? "Outgoing" : "Incoming"
				);
			}

			return result;
		}

		public async Task<bool> SendRequestAsync(string me, string to, CancellationToken ct = default)
		{
			if (me == to) return false;

			// Prevent dupes/duplicates
			var existing = await _db.UserRelations.FindAsync(new object?[] { me, to }, ct)
						   ?? await _db.UserRelations.FindAsync(new object?[] { to, me }, ct);

			if (existing != null)
			{
				if (existing.Status == UserRelationStatus.Accepted) return false;
				// if pending opposite way, accept directly (auto-accept mutual)
				if (existing.Status == UserRelationStatus.Pending && existing.UserId == to && existing.FriendId == me)
				{
					existing.Status = UserRelationStatus.Accepted;
					await _db.SaveChangesAsync(ct);
					return true;
				}
				// pending same direction or blocked -> reject
				return false;
			}

			// Check AllowFollow
			var followOk = await _db.PrivacySettings
				.Where(p => p.UserId == to)
				.Select(p => p.AllowFollow)
				.FirstOrDefaultAsync(ct);

			if (!followOk) return false;

			_db.UserRelations.Add(new UserRelation { UserId = me, FriendId = to, Status = UserRelationStatus.Pending });
			await _db.SaveChangesAsync(ct);
			return true;
		}

		public async Task<IReadOnlyList<FriendRequestItemDto>> GetIncomingAsync(string me, CancellationToken ct = default)
		{
			var q = await _db.UserRelations
				.Where(r => r.FriendId == me && r.Status == UserRelationStatus.Pending)
				.Select(r => new
				{
					r.UserId,
					r.User.FirstName,
					r.User.LastName,
					r.User.UserName
				})
				.ToListAsync(ct);

			return q.Select(x => new FriendRequestItemDto(
				x.UserId,
				$"{x.FirstName} {x.LastName}".Trim(),
				null
			)).ToList();
		}

		public async Task<bool> AcceptAsync(string me, string from, CancellationToken ct = default)
		{
			var rel = await _db.UserRelations.FindAsync(new object?[] { from, me }, ct);
			if (rel is null || rel.Status != UserRelationStatus.Pending) return false;
			rel.Status = UserRelationStatus.Accepted;
			await _db.SaveChangesAsync(ct);
			return true;
		}

		public async Task<bool> RejectAsync(string me, string from, CancellationToken ct = default)
		{
			var rel = await _db.UserRelations.FindAsync(new object?[] { from, me }, ct);
			if (rel is null || rel.Status != UserRelationStatus.Pending) return false;
			_db.UserRelations.Remove(rel);
			await _db.SaveChangesAsync(ct);
			return true;
		}

		public async Task<bool> CancelAsync(string me, string to, CancellationToken ct = default)
		{
			var rel = await _db.UserRelations.FindAsync(new object?[] { me, to }, ct);
			if (rel is null || rel.Status != UserRelationStatus.Pending) return false;
			_db.UserRelations.Remove(rel);
			await _db.SaveChangesAsync(ct);
			return true;
		}

		// For FriendService.cs
		public async Task<int> GetIncomingCountAsync(string me, CancellationToken ct = default)
		{
			return await _db.UserRelations
				.Where(r => r.FriendId == me && r.Status == UserRelationStatus.Pending)
				.CountAsync(ct);
		}

	}
}
