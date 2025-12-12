namespace APUS.Server.Domain.DTOs.Feature.Activity
{
	public enum ActivityType
	{
		Running,
		Hiking,
		Cycling,
		GpsRelatedActivity,
		MainActivity
		// ...
	}

	public sealed class EditActivityRequest
	{
		public required string Id { get; init; }
		public required string Title { get; init; }
		public string? Description { get; init; }
		public DateTime Date { get; init; }
		public ActivityType ActivityType { get; init; }
	}

}
