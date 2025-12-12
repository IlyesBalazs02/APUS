namespace APUS.Server.Domain.DTOs.Image
{
	public sealed class ActivityImageDto
	{
		public required string Url { get; init; }
		public double? Lat { get; init; }
		public double? Lon { get; init; }

		public DateTime? DateTaken { get; init; }
	}

}
