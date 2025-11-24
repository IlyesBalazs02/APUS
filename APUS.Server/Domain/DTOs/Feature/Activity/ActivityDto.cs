namespace APUS.Server.Domain.DTOs.Feature.Activity
{
	public record ActivityDto
	{
		public string Id { get; set; }
		public string Title { get; set; }
		public string? Description { get; set; }
		public TimeSpan Duration { get; set; }
		public DateTime Date {  get; set; }
		public int? AvgHr { get; set; }
		public int? TotalCalories { get; set; }
		public string Type { get; set; }
		public string? DisplayName { get; set; }
		public int? LikesCount { get; set; }
		public bool? IsLikedByCurrentUser { get; set; }

		//owner information
		public string UserFullName { get; set; }
		public string avatarUrl { get; set; }
	}

	public record GpsActivityDto : ActivityDto
	{
		public double? DistanceKm { get; set; }
		public double? ElevationGain { get; set; }
	}

	public record RunningActivityDto : GpsActivityDto
	{
		public double? Pace { get; set; }
	}
}
