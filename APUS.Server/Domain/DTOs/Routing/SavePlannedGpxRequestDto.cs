namespace APUS.Server.Domain.DTOs.Routing
{
	public sealed class SavePlannedGpxRequestDto
	{
		public string? FileName { get; set; }
		public List<RouteCoordinateDto>? Points { get; set; }
	}

}
