using APUS.Server.Domain.DTOs.Routing;

namespace APUS.Server.Services.Implementations.MapServices
{
	public interface IRoutingService
	{
		IReadOnlyList<RouteCoordinateDto> RouteBetweenCoords(double fromLat, double fromLon, double toLat, double toLon);
		SnapResponseDto SnapToRoad(double lat, double lon);
	}
}