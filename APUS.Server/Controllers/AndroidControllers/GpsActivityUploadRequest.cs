namespace APUS.Server.Controllers.AndroidControllers
{
	public sealed class GpsActivityUploadRequest
	{
		public string? Title { get; set; }
		public string? Description { get; set; }

		public string ActivityType { get; set; } = default!;
		public long StartTimeUnixSeconds { get; set; }
		public long DurationSeconds { get; set; }

		public double? TotalDistanceKm { get; set; }
		public double? TotalAscentMeters { get; set; }
		public double? TotalDescentMeters { get; set; }
		public double? AvgPace { get; set; }

		public long? FinishTimeUnixSeconds { get; set; }
	}

}
