namespace APUS.Server.Domain.DTOs.Groups
{
	public sealed class CreateGroupDto
	{
		public required string Name { get; set; }
		public string? Description { get; set; }
		public bool IsOpen { get; set; } = true;
	}

	public sealed class GroupDto
	{
		public long Id { get; set; }
		public required string Name { get; set; }
		public string? Description { get; set; }
		public bool IsOpen { get; set; }
		public string CreatedByUserId { get; set; } = null!;
		public DateTime CreatedAtUtc { get; set; }
		public int MemberCount { get; set; }

		public bool IsMember { get; set; }
		public bool IsAdmin { get; set; }

		public bool HasPendingJoinRequest { get; set; }
	}

	public sealed class UpdateGroupDto
	{
		public string? Name { get; set; }
		public string? Description { get; set; }
		public bool? IsOpen { get; set; }
	}

	public sealed class DecideJoinRequestDto
	{
		public bool Approve { get; set; }
	}

	public sealed class GroupMemberDto
	{
		public required string UserId { get; set; }
		public required string FullName { get; set; }
		public required string AvatarUrl { get; set; }
		public required string Role { get; set; }
		public DateTime JoinedAtUtc { get; set; }
	}

	public sealed class GroupJoinRequestDto
	{
		public long Id { get; set; }
		public long GroupId { get; set; }

		public required string RequesterUserId { get; set; }
		public required string RequesterFullName { get; set; }
		public string? RequesterAvatarUrl { get; set; }

		public DateTime RequestedAtUtc { get; set; }
	}

}
