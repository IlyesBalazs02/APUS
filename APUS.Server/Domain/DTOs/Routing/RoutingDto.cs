namespace APUS.Server.Domain.DTOs.Routing
{
	public sealed class RouteCoordinateDto
	{
		public double Lat { get; init; }
		public double Lon { get; init; }
	}

	public sealed class SnapResponseDto
	{
		public int NodeId { get; init; }
		public double Lat { get; init; }
		public double Lon { get; init; }
	}

	public sealed class RouteRequestDto
	{
		public double FromLat { get; init; }
		public double FromLon { get; init; }
		public double ToLat { get; init; }
		public double ToLon { get; init; }
	}
}
