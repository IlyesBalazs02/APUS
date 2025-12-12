using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public class UserRelationRepository : IUserRelationRepository
	{
		private readonly AppDbContext _db;
		private readonly IProfilePictureService _profilePictureService;

		public UserRelationRepository(AppDbContext db, IProfilePictureService profilePictureService)
		{
			_db = db;
			_profilePictureService = profilePictureService;
		}

		// Find a specific user–friend relation by primary key
		public Task<UserRelation?> FindAsync(string userId, string friendId, CancellationToken ct = default)
			=> _db.UserRelations.FindAsync(new object?[] { userId, friendId }, ct).AsTask();

		// Finds an existing relation in either direction between two users
		public async Task<UserRelation?> FindEitherDirectionAsync(string a, string b, CancellationToken ct = default)
			=> await FindAsync(a, b, ct) ?? await FindAsync(b, a, ct);

		// Gets the AllowFollow flag for one specific user
		public async Task<bool?> GetAllowFollowAsync(string userId, CancellationToken ct = default)
			=> await _db.PrivacySettings
				.Where(p => p.UserId == userId)
				.Select(p => (bool?)p.AllowFollow)
				.FirstOrDefaultAsync(ct);

		// Returns all relations between the current user and given target IDs
		public Task<List<UserRelation>> GetBetweenAsync(string me, IEnumerable<string> targetIds, CancellationToken ct = default)
		{
			var ids = targetIds.Distinct().ToArray();
			return _db.UserRelations
				.Where(r => (r.UserId == me && ids.Contains(r.FriendId)) ||
							(r.FriendId == me && ids.Contains(r.UserId)))
				.ToListAsync(ct);
		}

		// Gets AllowFollow flags for multiple users as a dictionary
		public async Task<Dictionary<string, bool?>> GetAllowFollowMapAsync(IEnumerable<string> userIds, CancellationToken ct = default)
		{
			var ids = userIds.Distinct().ToArray();
			var list = await _db.PrivacySettings
				.Where(p => ids.Contains(p.UserId))
				.Select(p => new { p.UserId, p.AllowFollow })
				.ToListAsync(ct);

			return list.ToDictionary(x => x.UserId, x => (bool?)x.AllowFollow);
		}

		// Adds a new user relation
		public async Task AddAsync(UserRelation relation, CancellationToken ct = default)
		{
			await _db.UserRelations.AddAsync(relation, ct);
		}

		// Removes an existing user relation
		public void Remove(UserRelation relation) => _db.UserRelations.Remove(relation);

		// Saves all pending changes
		public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

		// Returns all incoming (pending) friend requests for a user
		public async Task<IReadOnlyList<(string FromUserId, string FromFullName)>> GetIncomingAsync(string me, CancellationToken ct = default)
		{
			var q = await _db.UserRelations
				.Where(r => r.FriendId == me && r.Status == UserRelationStatus.Pending)
				.Select(r => new
				{
					r.UserId,
					Full = (r.User.FirstName + " " + r.User.LastName).Trim()
				})
				.ToListAsync(ct);

			return q.Select(x => (x.UserId, x.Full)).ToList();
		}

		public Task<int> GetIncomingCountAsync(string me, CancellationToken ct = default)
			=> _db.UserRelations.CountAsync(r => r.FriendId == me && r.Status == UserRelationStatus.Pending, ct);


		public async Task<PagedResponse<UserSearchDto>> GetFriendsPagedAsync(string me, string? query, int skip, int take, CancellationToken ct = default)
		{
			query = (query ?? "").Trim();

			var qSent = _db.UserRelations
				.Where(r => r.Status == UserRelationStatus.Accepted && r.UserId == me)
				.Select(r => r.Friend);

			var qRecv = _db.UserRelations
				.Where(r => r.Status == UserRelationStatus.Accepted && r.FriendId == me)
				.Select(r => r.User);

			var baseQ = qSent.Concat(qRecv);

			if (!string.IsNullOrWhiteSpace(query))
			{
				var qLower = query.ToLower();
				baseQ = baseQ.Where(u => (u.FirstName + " " + u.LastName).ToLower().Contains(qLower));
			}

			var ordered = baseQ.OrderBy(u => u.LastName).ThenBy(u => u.FirstName);

			var raw = await ordered
				.Skip(skip)
				.Take(take)
				.Select(u => new { u.Id, u.FirstName, u.LastName })
				.ToListAsync(ct);

			var items = new List<UserSearchDto>(raw.Count);
			foreach (var u in raw)
			{
				items.Add(new UserSearchDto
				{
					Id = u.Id,
					FullName = (u.FirstName + " " + u.LastName).Trim(),
					AvatarUrl = await _profilePictureService.GetProfilePictureUrlAsync(u.Id)
				});
			}

			var hasMore = raw.Count == take && await ordered.Skip(skip + take).AnyAsync(ct);

			return new PagedResponse<UserSearchDto> { Items = items, HasMore = hasMore };
		}


	}
}
