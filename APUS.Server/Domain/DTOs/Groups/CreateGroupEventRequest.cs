using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Domain.DTOs.Groups
{
	public sealed class CreateGroupEventRequest
	{
		[Required]
		[StringLength(200, MinimumLength = 1)]
		public string Title { get; set; } = null!;

		[StringLength(4000)]
		public string? Description { get; set; }

		public string? TrackActivityId { get; set; }

		public DateTime? StartsAtUtc { get; set; }
	}
}
