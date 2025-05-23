using APUS.Server.Routing;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations
{
	public class RouteService : IRouteService
	{
		public Task<List<(double latitude, double longitude)>> GetRouteAsync(
		(double lat, double lon) start,
		(double lat, double lon) end)
		=> new CreateRoute().GetRouteAsync(start, end);
	}
}
