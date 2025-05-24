using APUS.Server.Models;
using APUS.Server.Routing;
using APUS.Server.Services.Interfaces;
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
			public Coordinate Start { get; set; } = default!;
			public Coordinate End { get; set; } = default!;
		}

		public class Coordinate
		{
			public double Latitude { get; set; }
			public double Longitude { get; set; }
		}

		private readonly IRouteService _routeService;
		public RoutingController(IRouteService routeService)
			=> _routeService = routeService;

		[HttpPost("route")]
		public Task<List<(double latitude, double longitude)>> GetRoute([FromBody] RouteRequest request)
		{
			return _routeService.GetRouteAsync(
				(request.Start.Latitude, request.Start.Longitude),
				(request.End.Latitude, request.End.Longitude)
				);
		}
	}
}
