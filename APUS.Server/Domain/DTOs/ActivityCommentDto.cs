using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Domain.DTOs
{
	public sealed class ActivityCommentDto
	{
		public Guid Id { get; set; }

		public string AuthorUserId { get; set; } = null!;
		public string AuthorFullName { get; set; } = null!;
		public string? AuthorAvatarUrl { get; set; }

		public string Text { get; set; } = null!;
		public DateTime CreatedAtUtc { get; set; }
	}

	public sealed class CreateActivityCommentRequest
	{
		[Required]
		[MaxLength(1000)]
		public string Text { get; set; } = null!;
	}
}
