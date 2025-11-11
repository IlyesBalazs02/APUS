using APUS.Server.Domain.Models;

namespace APUS.Server.Domain.Entities.Groups
{
	// Domain/Entities/GroupMembership.cs
	public enum GroupRole
	{
		Member = 0,
		Admin = 1
	}

	public sealed class GroupMembership
	{
		public long GroupId { get; set; }
		public Group Group { get; set; } = null!;
		public string UserId { get; set; } = null!;
		public SiteUser User { get; set; } = null!;
		public GroupRole Role { get; set; } = GroupRole.Member;
		public DateTime JoinedAtUtc { get; set; }
	}

}
