using System.Diagnostics;

namespace APUS.Server.Domain.Models
{
	public class ActivityImage
	{
		public int Id { get; set; }

		public string ActivityId { get; set; } = null!;
		public MainActivity Activity { get; set; } = null!;

		public string FileName { get; set; } = null!;
		public string Url { get; set; } = null!; 
		public DateTime UploadedAt { get; set; }

		// EXIF fields (typed + indexable)
		public DateTime? DateTaken { get; set; }
		public double? GpsLat { get; set; }
		public double? GpsLon { get; set; }

		public string? RawMetadataJson { get; set; }
	}

}
