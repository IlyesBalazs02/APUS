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

		public Task<Group?> GetAsync(long id, CancellationToken ct) =>
			_db.Groups
			   .Include(x => x.Members)
			   .FirstOrDefaultAsync(x => x.Id == id, ct);

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
					MemberCount = g.Members.Count
				})
				.ToListAsync(ct);
		}

		public Task<bool> IsMemberAsync(long groupId, string userId, CancellationToken ct) =>
			_db.GroupMemberships.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, ct);

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

		public Task<bool> HasPendingRequestAsync(long groupId, string userId, CancellationToken ct) =>
			_db.GroupJoinRequests.AnyAsync(r =>
				r.GroupId == groupId && r.RequesterUserId == userId && r.Status == JoinRequestStatus.Pending, ct);

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

		public Task<GroupJoinRequest?> GetJoinRequestWithGroupAsync(long requestId, CancellationToken ct) =>
			_db.GroupJoinRequests
			   .Include(r => r.Group).ThenInclude(g => g.Members)
			   .FirstOrDefaultAsync(r => r.Id == requestId, ct);

		public async Task RemoveMemberAsync(long groupId, string userId, CancellationToken ct)
		{
			var m = await _db.GroupMemberships.FirstOrDefaultAsync(x => x.GroupId == groupId && x.UserId == userId, ct);
			if (m is null) return;
			_db.GroupMemberships.Remove(m);
			await _db.SaveChangesAsync(ct);
		}

		public Task<int> AdminCountAsync(long groupId, string exceptUserId, CancellationToken ct) =>
			_db.GroupMemberships.CountAsync(m => m.GroupId == groupId && m.UserId != exceptUserId && m.Role == GroupRole.Admin, ct);

		public async Task UpdateAsync(Group g, CancellationToken ct)
		{
			_db.Groups.Update(g);
			await _db.SaveChangesAsync(ct);
		}

		public IQueryable<GroupMembership> MembersQuery(long groupId) =>
			_db.GroupMemberships.AsNoTracking().Where(m => m.GroupId == groupId);

		public IQueryable<GroupJoinRequest> PendingRequestsQuery(long groupId) =>
			_db.GroupJoinRequests.AsNoTracking().Where(r => r.GroupId == groupId && r.Status == JoinRequestStatus.Pending);
	}
}
