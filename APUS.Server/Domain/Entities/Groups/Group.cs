using APUS.Server.Domain.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Domain.Entities.Groups
{
	public sealed class Group
	{
		public long Id { get; set; }
		public required string Name { get; set; } = null!;
		public string? Description { get; set; }
		public bool IsOpen { get; set; } = true; // need permission to join??

		public GroupPostPermission WhoCanPost { get; set; } = GroupPostPermission.Members;
		public GroupEventPermission WhoCanCreateEvent { get; set; } = GroupEventPermission.AdminsOnly;

		public string CreatedByUserId { get; set; } = null!;
		public SiteUser CreatedByUser { get; set; } = null!;
		public DateTime CreatedAtUtc { get; set; }

		public ICollection<GroupMembership> Members { get; set; } = new List<GroupMembership>();
		public ICollection<GroupJoinRequest> JoinRequests { get; set; } = new List<GroupJoinRequest>();

		public ICollection<GroupPost> Posts { get; set; } = new List<GroupPost>();

		public ICollection<GroupEvent> Events { get; set; } = new List<GroupEvent>();
	}

	public enum GroupPostPermission
	{
		AdminsOnly = 0,
		Members = 1
	}

	public enum GroupEventPermission
	{
		AdminsOnly = 0,
		Members = 1
	}

	public sealed class GroupPost
	{
		public long Id { get; set; }

		public long GroupId { get; set; }
		public Group Group { get; set; } = null!;

		public string AuthorUserId { get; set; } = null!;
		public SiteUser AuthorUser { get; set; } = null!;

		public required string Title { get; set; } = null!;
		public required string Text { get; set; } = null!;

		public DateTime CreatedAtUtc { get; set; }

		[BindNever]
		[ValidateNever]
		[System.Text.Json.Serialization.JsonIgnore]
		public ICollection<SiteUser> LikedBy { get; set; } = new List<SiteUser>();

		[BindNever]
		[ValidateNever]
		[System.Text.Json.Serialization.JsonIgnore]
		public ICollection<GroupPostComment> Comments { get; set; } = new List<GroupPostComment>();

	}

	public sealed class GroupPostComment : CommentBase
	{
		[Required]
		public long GroupPostId { get; set; }
		public GroupPost GroupPost { get; set; } = null!;
	}
}
