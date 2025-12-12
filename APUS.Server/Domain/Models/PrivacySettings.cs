using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Domain.Models
{
	public enum VisibilityLevel
	{
		Everyone = 0,
		Followers = 1,
		OnlyMe = 2
	}

	public class PrivacySettings
	{
		[Key]
		public Guid Id { get; set; } = Guid.NewGuid();

		[Required]
		public string UserId { get; set; }

		[ForeignKey(nameof(UserId))]
		public virtual SiteUser User { get; set; }

		public bool AllowFollow { get; set; } = true;

		public VisibilityLevel ActivityVisibility { get; set; } = VisibilityLevel.Everyone;

		public VisibilityLevel ProfileVisibility { get; set; } = VisibilityLevel.Everyone;

		public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
	}
}

