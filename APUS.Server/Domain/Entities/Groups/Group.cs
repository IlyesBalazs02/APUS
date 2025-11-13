using APUS.Server.Domain.Models;

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

}
