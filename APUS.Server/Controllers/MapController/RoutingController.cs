using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Implementations.MapServices;
using Microsoft.AspNetCore.Mvc;

namespace APUS.Server.Controllers.MapController
{
		[ApiController]
		[Route("api/[controller]")]
		public sealed class RoutingController : ControllerBase
		{
			private readonly IRoutingService _routing;

			public RoutingController(IRoutingService routing)
			{
				_routing = routing;
			}

			/// <summary>
			/// Snap a clicked point to the nearest road node.
			/// GET /api/routing/snap?lat=...&lon=...
			/// </summary>
			[HttpGet("snap")]
			public ActionResult<SnapResponseDto> Snap([FromQuery] double lat, [FromQuery] double lon)
			{
				var result = _routing.SnapToRoad(lat, lon);
				return Ok(result);
			}

			/// <summary>
			/// Route between two coordinates and return the polyline coordinates.
			/// POST /api/routing/route
			/// </summary>
			[HttpPost("route")]
			public ActionResult<IReadOnlyList<RouteCoordinateDto>> Route([FromBody] RouteRequestDto request)
			{
				if (!ModelState.IsValid)
					return ValidationProblem(ModelState);

				var coords = _routing.RouteBetweenCoords(
					request.FromLat, request.FromLon,
					request.ToLat, request.ToLon);

				return Ok(coords);
			}
		}
	
}
