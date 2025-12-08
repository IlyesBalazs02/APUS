using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Implementations.FileServices;
using APUS.Server.Services.Implementations.MapServices;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using System.Text;
using System.Xml.Linq;

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
		private readonly ITrackFileService _trackFileService;

		public RoutingController(
			IRoutingService routing,
			IHuberRegressor linearAggression,
			IWebHostEnvironment env,
			ISolarService solarService,
			ITrackFileService trackFileService)
		{
			_routing = routing;
			_huberRegression = linearAggression;
			_env = env;
			_solarService = solarService;
			_trackFileService = trackFileService;
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

		//TODO: _STORAGESERVICE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
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

		[HttpGet("tracks")]
		[Authorize]
		public ActionResult<IEnumerable<string>> GetUserTrackNames()
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var trackNames = _trackFileService.GetTrackNamesForUser(userId);

			return Ok(trackNames);
		}

		[HttpGet("tracks/{fileName}")]
		[Authorize]
		public ActionResult<List<CoordinateDto>> GetTrackPoints(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return BadRequest("File name is required.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			// Same folder as in SavePlannedGpx
			var tracksDir = Path.Combine(_env.WebRootPath, "Users", userId, "Tracks");
			if (!Directory.Exists(tracksDir))
				return NotFound("No Tracks folder for user.");

			// Reuse your ClearFileName sanitiser
			var baseName = ClearFileName(fileName);
			var filePath = Path.Combine(tracksDir, baseName + ".gpx");

			if (!System.IO.File.Exists(filePath))
				return NotFound("Track file not found.");

			try
			{
				var points = ParseGpxTrackPoints(filePath);
				if (points.Count == 0)
					return NotFound("No trackpoints found in GPX.");

				return Ok(points);
			}
			catch (Exception ex)
			{
				// You can log ex here
				return StatusCode(500, "Failed to parse GPX.");
			}
		}

		[HttpDelete("tracks/{fileName}")]
		[Authorize]
		public IActionResult DeleteTrack(string fileName)
		{
			if (string.IsNullOrWhiteSpace(fileName))
				return BadRequest("File name is required.");

			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			if (string.IsNullOrEmpty(userId))
				return Unauthorized();

			var tracksDir = Path.Combine(_env.WebRootPath, "Users", userId, "Tracks");
			if (!Directory.Exists(tracksDir))
				return NotFound("No Tracks folder for user.");

			var baseName = ClearFileName(fileName);
			var filePath = Path.Combine(tracksDir, baseName + ".gpx");

			if (!System.IO.File.Exists(filePath))
				return NotFound("Track file not found.");

			try
			{
				System.IO.File.Delete(filePath);
				return NoContent();
			}
			catch (Exception)
			{
				return StatusCode(500, "Failed to delete track.");
			}
		}



		private static List<CoordinateDto> ParseGpxTrackPoints(string path)
		{
			var result = new List<CoordinateDto>();

			var doc = XDocument.Load(path);
			// Give a default namespace for GPX 1.1; if your GPX has a different ns adjust here
			XNamespace ns = "http://www.topografix.com/GPX/1/1";

			// trkpt inside trk/trkseg
			var trkpts = doc.Descendants(ns + "trkpt");
			foreach (var p in trkpts)
			{
				var latAttr = p.Attribute("lat");
				var lonAttr = p.Attribute("lon");
				if (latAttr == null || lonAttr == null) continue;

				if (double.TryParse(latAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
					double.TryParse(lonAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
				{
					result.Add(new CoordinateDto
					{
						Lat = lat,
						Lon = lon
					});
				}
			}

			return result;
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
