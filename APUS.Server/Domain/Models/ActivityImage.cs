using System.Diagnostics;

namespace APUS.Server.Domain.Models
{
	public class ActivityImage
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();

		public string ActivityId { get; set; } = null!;
		public MainActivity Activity { get; set; } = null!;

		public string FileName { get; set; } = null!;
		public string Url { get; set; } = null!; 
		public DateTime UploadedAt { get; set; }

		// EXIF fields
		public DateTime? DateTaken { get; set; }
		public double? GpsLat { get; set; }
		public double? GpsLon { get; set; }

		public string? RawMetadataJson { get; set; }
	}

}
