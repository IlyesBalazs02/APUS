using APUS.Server.Domain.DTOs.Routing;

namespace APUS.Server.Services.Interfaces
{
	public interface IRoutingService
	{
		IReadOnlyList<RouteCoordinateDto> RouteBetweenCoords(double fromLat, double fromLon, double toLat, double toLon);
		IReadOnlyList<float?> SampleElevation(IReadOnlyList<RouteCoordinateDto> points);
		SnapResponseDto SnapToRoad(double lat, double lon);
	}
}