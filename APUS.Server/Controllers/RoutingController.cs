using APUS.Server.Models;
using APUS.Server.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OSMRouting;

namespace APUS.Server.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class RoutingController : ControllerBase
	{
		public class RouteRequest
		{
			public Coordinate Start { get; set; }
			public Coordinate End { get; set; }
		}

		public class Coordinate
		{
			public double Latitude { get; set; }
			public double Longitude { get; set; }
		}

		[HttpPost("route")]
		public async Task<List<(double latitude, double longitude)>> GetRoute([FromBody] RouteRequest request)
		{
			var createRoute = new CreateRoute();

			var route = await createRoute.GetRouteAsync(
				(request.Start.Latitude, request.Start.Longitude),
				(request.End.Latitude, request.End.Longitude)
			);

			return route;
		}
	}
}
