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
}
