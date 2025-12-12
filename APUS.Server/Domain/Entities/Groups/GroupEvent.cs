using APUS.Server.Domain.Models;
using System.ComponentModel.DataAnnotations;

namespace APUS.Server.Domain.Entities.Groups
{
	public sealed class GroupEvent
	{
		public long Id { get; set; }

		public long GroupId { get; set; }
		public Group Group { get; set; } = null!;

		public required string Title { get; set; } = null!;
		public string? Description { get; set; }

		public string? TrackActivityId { get; set; }
		public MainActivity? TrackActivity { get; set; }

		public string CreatedByUserId { get; set; } = null!;
		public SiteUser CreatedByUser { get; set; } = null!;

		public DateTime CreatedAtUtc { get; set; }
		public DateTime? StartsAtUtc { get; set; }

		public ICollection<GroupEventParticipant> Participants { get; set; }
			= new List<GroupEventParticipant>();
	}

	public sealed class GroupEventParticipant
	{
		public long GroupEventId { get; set; }
		public GroupEvent GroupEvent { get; set; } = null!;

		public string UserId { get; set; } = null!;
		public SiteUser User { get; set; } = null!;

		public DateTime JoinedAtUtc { get; set; }
	}

}
