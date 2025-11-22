namespace APUS.Server.Domain.DTOs.Routing
{
	public sealed class CoordinateDto
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
	}

	public sealed class SnapResponseDto
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
	}

	public sealed class RouteRequestDto
	{
		public double FromLat { get; set; }
		public double FromLon { get; set; }
		public double ToLat { get; set; }
		public double ToLon { get; set; }
	}

	public sealed class RouteCoordinateDto
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
	}
}
