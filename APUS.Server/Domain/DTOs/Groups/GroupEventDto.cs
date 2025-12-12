namespace APUS.Server.Domain.DTOs.Groups
{
	public sealed class GroupEventDto
	{
		public long Id { get; set; }
		public long GroupId { get; set; }

		public string Title { get; set; } = null!;
		public string? Description { get; set; }

		public string? TrackActivityId { get; set; }

		public string CreatedByUserId { get; set; } = null!;
		public string CreatedByFullName { get; set; } = null!;
		public string? CreatedByAvatarUrl { get; set; }

		public DateTime CreatedAtUtc { get; set; }
		public DateTime? StartsAtUtc { get; set; }

		public int ParticipantCount { get; set; }
		public bool IsJoinedByCurrentUser { get; set; }
	}

	public sealed class GroupEventParticipantDto
	{
		public required string UserId { get; init; }
		public required string FullName { get; init; }
		public string? AvatarUrl { get; init; }
		public DateTime JoinedAtUtc { get; init; }
	}
}
