using APUS.Server.Domain.Models;

namespace APUS.Server.Domain.Entities.Groups
{
	// Domain/Entities/GroupJoinRequest.cs
	public enum JoinRequestStatus
	{
		Pending = 0,
		Approved = 1,
		Rejected = 2
	}

	public sealed class GroupJoinRequest
	{
		public long Id { get; set; }
		public long GroupId { get; set; }
		public Group Group { get; set; } = null!;
		public string RequesterUserId { get; set; } = null!;
		public SiteUser RequesterUser { get; set; } = null!;
		public JoinRequestStatus Status { get; set; } = JoinRequestStatus.Pending;
		public DateTime CreatedAtUtc { get; set; }
		public string? DecidedByUserId { get; set; }
	}

}
