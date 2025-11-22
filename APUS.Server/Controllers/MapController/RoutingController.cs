using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Interfaces;
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

			[HttpGet("snap")]
			public ActionResult<SnapResponseDto> Snap([FromQuery] double lat, [FromQuery] double lon)
			{
				var result = _routing.SnapToRoad(lat, lon);
				return Ok(result);
			}

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

		[HttpPost("elevation")]
		public ActionResult<IReadOnlyList<float?>> Elevation([FromBody] List<RouteCoordinateDto> points)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			var result = _routing.SampleElevation(points);
			return Ok(result);
		}

	}

}
