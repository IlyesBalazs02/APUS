namespace APUS.Server.Domain.DTOs.Routing
{
	public sealed class DaylightMarkerDto
	{
		public double Lat { get; set; }
		public double Lon { get; set; }
		public double Progress { get; set; }
		public double SecondsFromStart { get; set; }
	}

	public sealed class DaylightRequestDto
	{
		public List<RouteCoordinateDto> Points { get; set; } = new();
		public DateTime? StartLocalTime { get; set; } 
	}

	public sealed class DaylightResponseDto
	{
		public double PredictedSeconds { get; set; }

		public DateTime StartTime { get; set; }
		public DateTime FinishTime { get; set; }

		public DateTime Sunrise { get; set; }
		public DateTime Sunset { get; set; }

		public double PercentBeforeNightfall { get; set; }

		public DaylightMarkerDto? SunriseMarker { get; set; }
		public DaylightMarkerDto? SunsetMarker { get; set; }
	}

}
