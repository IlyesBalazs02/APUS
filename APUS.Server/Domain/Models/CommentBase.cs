using APUS.Server.Domain.Entities.User;

namespace APUS.Server.Domain.Models
{
	public abstract class CommentBase
	{
		public Guid Id { get; set; }

		public string AuthorUserId { get; set; } = null!;
		public SiteUser AuthorUser { get; set; } = null!;

		public string Text { get; set; } = null!;
		public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
	}

}
