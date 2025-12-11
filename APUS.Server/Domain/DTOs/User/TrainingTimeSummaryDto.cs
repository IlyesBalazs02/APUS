namespace APUS.Server.Domain.DTOs.User
{
	public class TrainingSportSummaryDto
	{
		public string ActivityType { get; set; } = default!;
		public double TotalHours { get; set; }
		public int ActivityCount { get; set; }
	}

	public class TrainingTimeSummaryDto
	{
		public string UserId { get; set; } = default!;
		public string Period { get; set; } = default!;   // "LastWeek", "LastMonth", "LastYear"
		public DateTime FromUtc { get; set; }
		public DateTime ToUtc { get; set; }
		public double TotalHours { get; set; }
		public int ActivityCount { get; set; }

		public List<TrainingSportSummaryDto> Sports { get; set; } = new();
	}
}
