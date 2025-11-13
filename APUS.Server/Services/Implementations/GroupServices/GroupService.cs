using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace APUS.Server.Services.Implementations.GroupServices
{
	public class GroupService : IGroupService
	{
		private readonly IGroupRepository _repo;
		public GroupService(IGroupRepository repo) => _repo = repo;

		public async Task<GroupDto> CreateAsync(string creatorId, CreateGroupDto dto, CancellationToken ct)
		{
			var now = DateTime.UtcNow;
			var g = new Group
			{
				Name = dto.Name.Trim(),
				Description = dto.Description?.Trim(),
				IsOpen = dto.IsOpen,
				CreatedAtUtc = now,
				CreatedByUserId = creatorId,
				Members = new List<GroupMembership>
			{
				new() { UserId = creatorId, Role = GroupRole.Admin, JoinedAtUtc = now }
			}
			};
			await _repo.CreateAsync(g, ct);

			// map after save
			return new GroupDto
			{
				Id = g.Id,
				Name = g.Name,
				Description = g.Description,
				IsOpen = g.IsOpen,
				CreatedByUserId = g.CreatedByUserId,
				CreatedAtUtc = g.CreatedAtUtc,
				MemberCount = g.Members.Count
			};
		}

		//new getASync
		public async Task<GroupDto?> GetForUserAsync(long id, string viewerId, CancellationToken ct)
		{
			var g = await _repo.GetAsync(id, ct);
			if (g is null) return null;

			var isMember = g.Members.Any(m => m.UserId == viewerId);
			var isAdmin = g.Members.Any(m => m.UserId == viewerId && m.Role == GroupRole.Admin);

			// check if the current user has a pending join request (only interesting if not member)
			var hasPending = !isMember && await _repo.HasPendingRequestAsync(id, viewerId, ct);

			return new GroupDto
			{
				Id = g.Id,
				Name = g.Name,
				Description = g.Description,
				IsOpen = g.IsOpen,
				CreatedByUserId = g.CreatedByUserId,
				CreatedAtUtc = g.CreatedAtUtc,
				MemberCount = g.Members.Count,
				IsMember = isMember,
				IsAdmin = isAdmin,
				HasPendingJoinRequest = hasPending
			};
		}

		public async Task<GroupDto?> GetAsync(long id, CancellationToken ct)
		{
			var g = await _repo.GetAsync(id, ct);
			return g is null ? null : new GroupDto
			{
				Id = g.Id,
				Name = g.Name,
				Description = g.Description,
				IsOpen = g.IsOpen,
				CreatedByUserId = g.CreatedByUserId,
				CreatedAtUtc = g.CreatedAtUtc,
				MemberCount = g.Members.Count
			};
		}

		public Task<List<GroupDto>> SearchAsync(string? q, int skip, int take, CancellationToken ct)
			=> _repo.SearchAsync(q, skip, take, ct);

		public async Task RequestToJoinAsync(string userId, long groupId, CancellationToken ct)
		{
			var group = await _repo.GetAsync(groupId, ct)
				?? throw new KeyNotFoundException("Group not found");

			// already member? nothing to do
			if (group.Members.Any(m => m.UserId == userId))
				return;

			// OPEN group → just add membership once
			if (group.IsOpen)
			{
				// avoid duplicate membership
				var alreadyMember = await _repo.IsMemberAsync(groupId, userId, ct);
				if (!alreadyMember)
				{
					await _repo.AddMemberAsync(groupId, userId, GroupRole.Member, DateTime.UtcNow, ct);
				}
				return;
			}

			// CLOSED group → work with join requests
			var existing = await _repo.GetJoinRequestAsync(groupId, userId, ct);

			// if already have a pending request → do nothing
			if (existing is not null && existing.Status == JoinRequestStatus.Pending)
				return;

			var now = DateTime.UtcNow;

			if (existing is not null)
			{
				// reuse old row (Approved/Rejected) → reset to pending
				existing.Status = JoinRequestStatus.Pending;
				existing.CreatedAtUtc = now;
				existing.DecidedAtUtc = null;
				existing.DecidedByUserId = null;

				await _repo.UpdateJoinRequestAsync(existing, ct); // small helper, see below
			}
			else
			{
				// no request yet → create new
				await _repo.AddJoinRequestAsync(groupId, userId, now, ct);
			}
		}


		public async Task ApproveOrRejectAsync(string adminId, long requestId, bool approve, CancellationToken ct)
		{
			var req = await _repo.GetJoinRequestWithGroupAsync(requestId, ct)
					  ?? throw new KeyNotFoundException("Request not found");

			var isAdmin = req.Group.Members.Any(m => m.UserId == adminId && m.Role == GroupRole.Admin);
			if (!isAdmin) throw new UnauthorizedAccessException("Only admins can decide");

			if (req.Status != JoinRequestStatus.Pending) return;

			req.DecidedByUserId = adminId;
			req.Status = approve ? JoinRequestStatus.Approved : JoinRequestStatus.Rejected;

			if (approve && !req.Group.Members.Any(m => m.UserId == req.RequesterUserId))
				await _repo.AddMemberAsync(req.GroupId, req.RequesterUserId, GroupRole.Member, DateTime.UtcNow, ct);
			else
				await _repo.UpdateAsync(req.Group, ct); // persist decision (no member add)
		}

		public async Task LeaveAsync(string userId, long groupId, CancellationToken ct)
		{
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");
			var me = g.Members.FirstOrDefault(m => m.UserId == userId);
			if (me is null) return;

			if (me.Role == GroupRole.Admin)
			{
				var otherAdmins = await _repo.AdminCountAsync(groupId, userId, ct);
				if (otherAdmins == 0)
					throw new InvalidOperationException("Transfer admin to someone else before leaving");
			}

			await _repo.RemoveMemberAsync(groupId, userId, ct);
		}

		public async Task UpdateAsync(string adminId, long groupId, UpdateGroupDto dto, CancellationToken ct)
		{
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");
			var isAdmin = g.Members.Any(m => m.UserId == adminId && m.Role == GroupRole.Admin);
			if (!isAdmin) throw new UnauthorizedAccessException("Only admins can update group");

			if (!string.IsNullOrWhiteSpace(dto.Name)) g.Name = dto.Name.Trim();
			if (dto.Description is not null) g.Description = dto.Description.Trim();
			if (dto.IsOpen is not null) g.IsOpen = dto.IsOpen.Value;

			await _repo.UpdateAsync(g, ct);
		}

		public async Task<List<GroupMemberDto>> GetMembersAsync(long groupId, CancellationToken ct)
		{
			var q = _repo.MembersQuery(groupId)
						 .Include(m => m.User)
						 .Select(m => new GroupMemberDto
						 {
							 UserId = m.UserId,
							 FullName = m.User.FirstName + " " + m.User.LastName,
							 AvatarUrl = m.User.AvatarUrl,
							 Role = m.Role.ToString(),
							 JoinedAtUtc = m.JoinedAtUtc
						 });

			return await q.ToListAsync(ct);
		}

		public async Task KickAsync(string adminId, long groupId, string targetUserId, CancellationToken ct)
		{
			if (adminId == targetUserId)
				throw new InvalidOperationException("You cannot remove yourself.");

			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");

			var isAdmin = g.Members.Any(m => m.UserId == adminId && m.Role == GroupRole.Admin);
			if (!isAdmin) throw new UnauthorizedAccessException("Only admins can remove members.");

			var target = g.Members.FirstOrDefault(m => m.UserId == targetUserId);
			if (target is null) return; // already not a member

			// If target is Admin, ensure at least one other admin remains
			if (target.Role == GroupRole.Admin)
			{
				var otherAdmins = await _repo.AdminCountAsync(groupId, targetUserId, ct);
				if (otherAdmins == 0)
					throw new InvalidOperationException("Cannot remove the last admin.");
			}

			await _repo.RemoveMemberAsync(groupId, targetUserId, ct);
		}

		public async Task<List<GroupJoinRequestDto>> GetPendingRequestsAsync(string adminId, long groupId, CancellationToken ct)
		{
			// Ensure caller is admin of that group
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");
			var isAdmin = g.Members.Any(m => m.UserId == adminId && m.Role == GroupRole.Admin);
			if (!isAdmin)
				throw new UnauthorizedAccessException("Only admins can view join requests.");

			// Query pending requests with requester user loaded
			var q = _repo.JoinRequestsQuery(groupId)              // new repo helper, see step 4
						 .Include(r => r.RequesterUser)
						 .Where(r => r.Status == JoinRequestStatus.Pending)
						 .OrderByDescending(r => r.CreatedAtUtc)
						 .Select(r => new GroupJoinRequestDto
						 {
							 Id = r.Id,
							 GroupId = r.GroupId,
							 RequesterUserId = r.RequesterUserId,
							 RequesterFullName = r.RequesterUser.FirstName + " " + r.RequesterUser.LastName,
							 RequesterAvatarUrl = r.RequesterUser.AvatarUrl,
							 RequestedAtUtc = r.CreatedAtUtc
						 });

			return await q.ToListAsync(ct);
		}


	}
}
