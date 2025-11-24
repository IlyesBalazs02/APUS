using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Implementations.FileServices;
using APUS.Server.Services.Implementations.MapServices;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace APUS.Server.Controllers.MapController
{
	[ApiController]
	[Route("api/[controller]")]
	public sealed class RoutingController : ControllerBase
	{
		private readonly IRoutingService _routing;
		private readonly IHuberRegressor _huberRegression;
		private readonly IWebHostEnvironment _env;
		private readonly ISolarService _solarService;

		public RoutingController(
			IRoutingService routing,
			IHuberRegressor linearAggression,
			IWebHostEnvironment env,
			ISolarService solarService)
		{
			_routing = routing;
			_huberRegression = linearAggression;
			_env = env;
			_solarService = solarService;
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

		[HttpPost("predict-daylight")]
		public async Task<ActionResult<DaylightResponseDto>> PredictDaylight(
			[FromBody] DaylightRequestDto request)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			if (request.Points == null || request.Points.Count < 2)
				return BadRequest("At least two points are required.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var result = await _solarService.PredictDaylightAsync(request, userId);
			if (result is null)
				return StatusCode(500, "Prediction failed.");

			return Ok(result);
		}


		[HttpPost("predict-time")]
		public async Task<ActionResult<double>> PredictTime([FromBody] List<RouteCoordinateDto> points)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			if (points == null || points.Count < 2)
				return BadRequest("At least two points are required.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			// Temp GPX path inside the user's LA folder
			var laDir = Path.Combine(_env.WebRootPath, "Users", userId, "LAModels");
			Directory.CreateDirectory(laDir);
			var tempGpxPath = Path.Combine(laDir, $"planned_{Guid.NewGuid():N}.gpx");

			try
			{
				var elevs = _routing.SampleElevation(points);
				WriteGpx(tempGpxPath, points, elevs);

				var seconds = await _huberRegression.PredictTotalTimeSecondsAsync(userId, tempGpxPath);
				if (seconds == null)
					return StatusCode(500, "Prediction failed.");

				return Ok(seconds.Value); // seconds
			}
			finally
			{
				try { System.IO.File.Delete(tempGpxPath); } catch { /* ignore */ }
			}
		}

		[HttpPost("save-planned-gpx")]
		[Authorize]
		public ActionResult SavePlannedGpx([FromBody] SavePlannedGpxRequestDto request)
		{
			if (!ModelState.IsValid)
				return ValidationProblem(ModelState);

			if (request.Points == null || request.Points.Count < 2)
				return BadRequest("At least two points are required.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var tracksDir = Path.Combine(_env.WebRootPath, "Users", userId, "Tracks");
			Directory.CreateDirectory(tracksDir);

			var baseName = ClearFileName(request.FileName);
			if (string.IsNullOrWhiteSpace(baseName))
			{
				baseName = "Route_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
			}

			var filePath = Path.Combine(tracksDir, baseName + ".gpx");

			var elevations = _routing.SampleElevation(request.Points);

			WriteGpx(filePath, request.Points, elevations);

			return Ok();
		}


		private static void WriteGpx(
			string path,
			IReadOnlyList<RouteCoordinateDto> points,
			IReadOnlyList<float?>? elevations)
		{
			using var sw = new StreamWriter(path, false, Encoding.UTF8);
			sw.WriteLine(@"<?xml version=""1.0"" encoding=""UTF-8""?>");
			sw.WriteLine(@"<gpx version=""1.1"" creator=""APUS"" xmlns=""http://www.topografix.com/GPX/1/1"">");
			sw.WriteLine("<trk><name>Planned route</name><trkseg>");

			for (int i = 0; i < points.Count; i++)
			{
				var p = points[i];
				sw.Write($@"<trkpt lat=""{p.Lat:F7}"" lon=""{p.Lon:F7}"">");

				if (elevations != null && i < elevations.Count && elevations[i].HasValue)
				{
					sw.Write($"<ele>{elevations[i]!.Value:F1}</ele>");
				}

				sw.WriteLine("</trkpt>");
			}

			sw.WriteLine("</trkseg></trk></gpx>");
		}

		private static string ClearFileName(string? name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return string.Empty;

			var baseName = name.Trim();

			// Strip .gpx extension if user typed it
			if (baseName.EndsWith(".gpx", StringComparison.OrdinalIgnoreCase))
			{
				baseName = baseName.Substring(0, baseName.Length - 4);
			}

			var invalid = Path.GetInvalidFileNameChars();
			foreach (var ch in invalid)
			{
				baseName = baseName.Replace(ch, '_');
			}

			return baseName;
		}


	}

}
