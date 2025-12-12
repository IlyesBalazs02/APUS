using APUS.Server.Data.Repositories.Interfaces;
using APUS.Server.Domain.DTOs.Feature.Search;
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

		// Creates a new group and automatically adds the creator as admin.
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

				WhoCanPost = GroupPostPermission.Members,
				WhoCanCreateEvent = GroupEventPermission.AdminsOnly,

				Members = new List<GroupMembership>
				{
					new() { UserId = creatorId, Role = GroupRole.Admin, JoinedAtUtc = now }
				}
			};
			await _repo.CreateAsync(g, ct);

			return new GroupDto
			{
				Id = g.Id,
				Name = g.Name,
				Description = g.Description,
				IsOpen = g.IsOpen,
				CreatedByUserId = g.CreatedByUserId,
				CreatedAtUtc = g.CreatedAtUtc,
				MemberCount = g.Members.Count,
				WhoCanPost = g.WhoCanPost,
				WhoCanCreateEvent = g.WhoCanCreateEvent
			};
		}

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
				HasPendingJoinRequest = hasPending,
				WhoCanPost = g.WhoCanPost,
				WhoCanCreateEvent = g.WhoCanCreateEvent
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
				MemberCount = g.Members.Count,
				WhoCanPost = g.WhoCanPost,
				WhoCanCreateEvent = g.WhoCanCreateEvent
			};
		}

		public Task<List<GroupDto>> SearchAsync(string? q, int skip, int take, CancellationToken ct)
			=> _repo.SearchAsync(q, skip, take, ct);

		// Handles joining logic depending on whether the group is open or closed.
		public async Task RequestToJoinAsync(string userId, long groupId, CancellationToken ct)
		{
			var group = await _repo.GetAsync(groupId, ct)
				?? throw new KeyNotFoundException("Group not found");

			// already member? nothing to do
			if (group.Members.Any(m => m.UserId == userId))
				return;

			// OPEN group -> just add membership once
			if (group.IsOpen)
			{
				var alreadyMember = await _repo.IsMemberAsync(groupId, userId, ct);
				if (!alreadyMember)
				{
					await _repo.AddMemberAsync(groupId, userId, GroupRole.Member, DateTime.UtcNow, ct);
				}
				return;
			}

			// CLOSED group -> work with join requests
			var existing = await _repo.GetJoinRequestAsync(groupId, userId, ct);

			// if already have a pending request -> do nothing
			if (existing is not null && existing.Status == JoinRequestStatus.Pending)
				return;

			var now = DateTime.UtcNow;

			if (existing is not null)
			{
				existing.Status = JoinRequestStatus.Pending;
				existing.CreatedAtUtc = now;
				existing.DecidedAtUtc = null;
				existing.DecidedByUserId = null;

				await _repo.UpdateJoinRequestAsync(existing, ct);
			}
			else
			{
				await _repo.AddJoinRequestAsync(groupId, userId, now, ct);
			}
		}

		// Approves or rejects a join request and adds the user as member if approved.
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
				await _repo.UpdateAsync(req.Group, ct);
		}

		// Removes the current user from a group
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

		// Updates name/description/open-status, allowed only for group admins
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

		// Returns a list of all members of the given group.
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
			if (target is null) return;

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
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");
			var isAdmin = g.Members.Any(m => m.UserId == adminId && m.Role == GroupRole.Admin);
			if (!isAdmin)
				throw new UnauthorizedAccessException("Only admins can view join requests.");

			var q = _repo.JoinRequestsQuery(groupId)
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

		public async Task<GroupSettingsDto> GetSettingsAsync(string userId, long groupId, CancellationToken ct)
		{
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");

			var isAdmin = g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin);
			if (!isAdmin)
				throw new UnauthorizedAccessException("Only admins can view group settings.");

			return new GroupSettingsDto
			{
				GroupId = g.Id,
				Name = g.Name,
				Description = g.Description,
				IsOpen = g.IsOpen,
				WhoCanPost = g.WhoCanPost,
				WhoCanCreateEvent = g.WhoCanCreateEvent
			};
		}

		public async Task UpdateSettingsAsync(string userId, long groupId, UpdateGroupSettingsDto dto, CancellationToken ct)
		{
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");

			var isAdmin = g.Members.Any(m => m.UserId == userId && m.Role == GroupRole.Admin);
			if (!isAdmin)
				throw new UnauthorizedAccessException("Only admins can update group settings.");

			if (!string.IsNullOrWhiteSpace(dto.Name))
				g.Name = dto.Name.Trim();

			if (dto.Description is not null)
				g.Description = dto.Description.Trim();

			if (dto.IsOpen is not null)
				g.IsOpen = dto.IsOpen.Value;

			if (dto.WhoCanPost is not null)
				g.WhoCanPost = dto.WhoCanPost.Value;

			if (dto.WhoCanCreateEvent is not null)
				g.WhoCanCreateEvent = dto.WhoCanCreateEvent.Value;

			await _repo.UpdateAsync(g, ct);
		}

		#region Posts
		public async Task<PagedResponse<GroupPostDto>> GetPostsAsync(string viewerId,long groupId,int skip,int take,CancellationToken ct)
		{
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");

			var isMember = g.Members.Any(m => m.UserId == viewerId);

			if (!g.IsOpen && !isMember)
				throw new UnauthorizedAccessException("You are not allowed to view posts in this group.");

			var query = _repo.PostsQuery(groupId)
							 .Include(p => p.AuthorUser)
							 .OrderByDescending(p => p.CreatedAtUtc)
							 .ThenByDescending(p => p.Id);

			var list = await query
				.Skip(skip)
				.Take(take + 1)
				.ToListAsync(ct);

			var hasMore = list.Count == take + 1;
			if (hasMore)
				list.RemoveAt(list.Count - 1);

			var items = list.Select(p => new GroupPostDto
			{
				Id = p.Id,
				GroupId = p.GroupId,
				AuthorUserId = p.AuthorUserId,
				AuthorFullName = p.AuthorUser.FirstName + " " + p.AuthorUser.LastName,
				AuthorAvatarUrl = p.AuthorUser.AvatarUrl,
				Title = p.Title,
				Text = p.Text,
				CreatedAtUtc = p.CreatedAtUtc
			}).ToList();

			return new PagedResponse<GroupPostDto>
			{
				Items = items,
				HasMore = hasMore
			};
		}

		public async Task<GroupPostDto> CreatePostAsync(string authorId,long groupId,CreateGroupPostDto dto,CancellationToken ct)
		{
			var g = await _repo.GetAsync(groupId, ct) ?? throw new KeyNotFoundException("Group not found");

			var membership = g.Members.FirstOrDefault(m => m.UserId == authorId);
			var isMember = membership is not null;
			var isAdmin = membership?.Role == GroupRole.Admin;

			var canPost = g.WhoCanPost switch
			{
				GroupPostPermission.AdminsOnly => isAdmin,
				GroupPostPermission.Members => isMember,
				_ => false
			};

			if (!canPost)
				throw new UnauthorizedAccessException("You are not allowed to create posts in this group.");

			var now = DateTime.UtcNow;
			var post = new GroupPost
			{
				GroupId = groupId,
				AuthorUserId = authorId,
				Title = dto.Title.Trim(),
				Text = dto.Text.Trim(),
				CreatedAtUtc = now
			};

			await _repo.AddPostAsync(post, ct);

			var loaded = await _repo.PostsQuery(groupId)
									.Include(p => p.AuthorUser)
									.FirstAsync(p => p.Id == post.Id, ct);

			return new GroupPostDto
			{
				Id = loaded.Id,
				GroupId = loaded.GroupId,
				AuthorUserId = loaded.AuthorUserId,
				AuthorFullName = loaded.AuthorUser.FirstName + " " + loaded.AuthorUser.LastName,
				AuthorAvatarUrl = loaded.AuthorUser.AvatarUrl,
				Title = loaded.Title,
				Text = loaded.Text,
				CreatedAtUtc = loaded.CreatedAtUtc
			};
		}

		public async Task DeletePostAsync(string userId, long postId, CancellationToken ct)
		{
			var post = await _repo.GetPostWithGroupAsync(postId, ct)
					   ?? throw new KeyNotFoundException("Post not found");

			var g = post.Group;
			var membership = g.Members.FirstOrDefault(m => m.UserId == userId);
			var isAdmin = membership?.Role == GroupRole.Admin;
			var isAuthor = post.AuthorUserId == userId;

			if (!isAdmin && !isAuthor)
				throw new UnauthorizedAccessException("You are not allowed to delete this post.");

			await _repo.DeletePostAsync(post, ct);
		}
		#endregion

	}
}
