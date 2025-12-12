using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Data.Repositories.Implementations
{
	public class GroupRepository : IGroupRepository
	{
		private readonly AppDbContext _db;
		public GroupRepository(AppDbContext db) => _db = db;

		public async Task<Group> CreateAsync(Group g, CancellationToken ct)
		{
			_db.Groups.Add(g);
			await _db.SaveChangesAsync(ct);
			return g;
		}

		// Loads a group with member list included.
		public Task<Group?> GetAsync(long id, CancellationToken ct) =>
			_db.Groups
			   .Include(x => x.Members)
			   .FirstOrDefaultAsync(x => x.Id == id, ct);

		// Searches groups by name.
		public async Task<List<GroupDto>> SearchAsync(string? q, int skip, int take, CancellationToken ct)
		{
			q = q?.Trim();
			var qry = _db.Groups.AsNoTracking();
			if (!string.IsNullOrWhiteSpace(q))
				qry = qry.Where(g => EF.Functions.Like(g.Name, $"%{q}%"));

			return await qry
				.OrderByDescending(g => g.Id)
				.Skip(skip).Take(take)
				.Select(g => new GroupDto
				{
					Id = g.Id,
					Name = g.Name,
					Description = g.Description,
					IsOpen = g.IsOpen,
					CreatedByUserId = g.CreatedByUserId,
					CreatedAtUtc = g.CreatedAtUtc,
					MemberCount = g.Members.Count,

					IsMember = false,
					IsAdmin = false,
					HasPendingJoinRequest = false,
					WhoCanPost = g.WhoCanPost,
					WhoCanCreateEvent = g.WhoCanCreateEvent
				})
				.ToListAsync(ct);
		}

		// Checks whether a user is a member of a group
		public Task<bool> IsMemberAsync(long groupId, string userId, CancellationToken ct) =>
			_db.GroupMemberships.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, ct);

		// Inserts a new group membership
		public async Task AddMemberAsync(long groupId, string userId, GroupRole role, DateTime joinedAtUtc, CancellationToken ct)
		{
			_db.GroupMemberships.Add(new GroupMembership
			{
				GroupId = groupId,
				UserId = userId,
				Role = role,
				JoinedAtUtc = joinedAtUtc
			});
			await _db.SaveChangesAsync(ct);
		}

		// Checks if a user has a pending join request
		public Task<bool> HasPendingRequestAsync(long groupId, string userId, CancellationToken ct) =>
			_db.GroupJoinRequests.AnyAsync(r =>
				r.GroupId == groupId && r.RequesterUserId == userId && r.Status == JoinRequestStatus.Pending, ct);

		// Creates a new join request
		public async Task<GroupJoinRequest> AddJoinRequestAsync(long groupId, string userId, DateTime nowUtc, CancellationToken ct)
		{
			var req = new GroupJoinRequest
			{
				GroupId = groupId,
				RequesterUserId = userId,
				Status = JoinRequestStatus.Pending,
				CreatedAtUtc = nowUtc
			};
			_db.GroupJoinRequests.Add(req);
			await _db.SaveChangesAsync(ct);
			return req;
		}

		// Loads a join request with its related group and members
		public Task<GroupJoinRequest?> GetJoinRequestWithGroupAsync(long requestId, CancellationToken ct) =>
			_db.GroupJoinRequests
			   .Include(r => r.Group).ThenInclude(g => g.Members)
			   .FirstOrDefaultAsync(r => r.Id == requestId, ct);

		// Removes a user from a group
		public async Task RemoveMemberAsync(long groupId, string userId, CancellationToken ct)
		{
			var m = await _db.GroupMemberships.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId, ct);
			if (m is null) return;
			_db.GroupMemberships.Remove(m);
			await _db.SaveChangesAsync(ct);
		}

		// Returns number of admins except the given user
		public Task<int> AdminCountAsync(long groupId, string exceptUserId, CancellationToken ct) =>
			_db.GroupMemberships.CountAsync(m => m.GroupId == groupId && m.UserId != exceptUserId && m.Role == GroupRole.Admin, ct);

		// Saves updates to a group entity
		public async Task UpdateAsync(Group g, CancellationToken ct)
		{
			_db.Groups.Update(g);
			await _db.SaveChangesAsync(ct);
		}

		// Returns a query for group members
		public IQueryable<GroupMembership> MembersQuery(long groupId) =>
			_db.GroupMemberships.AsNoTracking().Where(m => m.GroupId == groupId);

		// Returns a query for pending join requests of a group
		public IQueryable<GroupJoinRequest> PendingRequestsQuery(long groupId) =>
			_db.GroupJoinRequests.AsNoTracking().Where(r => r.GroupId == groupId && r.Status == JoinRequestStatus.Pending);

		// Returns a query for all join requests of a group
		public IQueryable<GroupJoinRequest> JoinRequestsQuery(long groupId)
			=> _db.GroupJoinRequests
				   .AsNoTracking()
				   .Where(r => r.GroupId == groupId);

		// Gets a join request for a specific user and group.
		public Task<GroupJoinRequest?> GetJoinRequestAsync(long groupId, string userId, CancellationToken ct) =>
			_db.GroupJoinRequests
			   .FirstOrDefaultAsync(r => r.GroupId == groupId && r.RequesterUserId == userId, ct);

		// Saves changes to an existing join request record.
		public async Task UpdateJoinRequestAsync(GroupJoinRequest request, CancellationToken ct)
		{
			_db.GroupJoinRequests.Update(request);
			await _db.SaveChangesAsync(ct);
		}

		#region Posts
		// Returns queryable posts of a group
		public IQueryable<GroupPost> PostsQuery(long groupId) =>
			_db.GroupPosts.AsNoTracking().Where(p => p.GroupId == groupId);

		// Adds a new post
		public async Task AddPostAsync(GroupPost post, CancellationToken ct)
		{
			_db.GroupPosts.Add(post);
			await _db.SaveChangesAsync(ct);
		}

		// Loads a post with its group & members
		public Task<GroupPost?> GetPostWithGroupAsync(long postId, CancellationToken ct) =>
			_db.GroupPosts
			   .Include(p => p.Group).ThenInclude(g => g.Members)
			   .FirstOrDefaultAsync(p => p.Id == postId, ct);

		public async Task DeletePostAsync(GroupPost post, CancellationToken ct)
		{
			_db.GroupPosts.Remove(post);
			await _db.SaveChangesAsync(ct);
		}
		#endregion
	}
}
