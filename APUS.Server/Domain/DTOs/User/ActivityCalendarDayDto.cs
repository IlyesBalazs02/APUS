namespace APUS.Server.Domain.DTOs.User
{
	public class ActivityCalendarDayDto
	{
		public int Day { get; set; }
		public double TotalHours { get; set; }
		public int ActivityCount { get; set; }
	}

	public class ActivityCalendarMonthDto
	{
		public string UserId { get; set; } = default!;
		public int Year { get; set; }
		public int Month { get; set; }
		public List<ActivityCalendarDayDto> Days { get; set; } = new();
	}
}
