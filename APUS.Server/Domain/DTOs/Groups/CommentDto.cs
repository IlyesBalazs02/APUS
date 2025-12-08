namespace APUS.Server.Domain.DTOs.Groups
{
	public sealed class CommentDto
	{
		public Guid Id { get; set; }

		public string AuthorUserId { get; set; } = null!;
		public string AuthorFullName { get; set; } = null!;
		public string? AuthorAvatarUrl { get; set; }

		public string Text { get; set; } = null!;
		public DateTime CreatedAtUtc { get; set; }
	}
}
