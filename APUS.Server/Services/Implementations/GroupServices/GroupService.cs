using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Groups;
using APUS.Server.Domain.Entities.Groups;
using APUS.Server.Services.Interfaces;

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
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");

			if (await _repo.IsMemberAsync(groupId, userId, ct))
				return;

			if (g.IsOpen)
			{
				await _repo.AddMemberAsync(groupId, userId, GroupRole.Member, DateTime.UtcNow, ct);
				return;
			}

			if (await _repo.HasPendingRequestAsync(groupId, userId, ct))
				return;

			_ = await _repo.AddJoinRequestAsync(groupId, userId, DateTime.UtcNow, ct);
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
	}
}
