using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Interfaces;
using Innovative.SolarCalculator;
using System.Text;

namespace APUS.Server.Services.Implementations.MapServices
{
	public interface ISolarService
	{
		(DateTime Sunrise, DateTime Sunset) GetSolarTimes(DateTime dateTimeLocal, double lat, double lon);
		Task<DaylightResponseDto?> PredictDaylightAsync(DaylightRequestDto request, string userId);

	}

	public class SolarService : ISolarService
	{
		private readonly IRoutingService _routing;
		private readonly IHuberRegressor _huberRegression;
		private readonly IWebHostEnvironment _env;

		public SolarService(IRoutingService routing, IHuberRegressor huberRegression, IWebHostEnvironment env)
		{
			_routing = routing;
			_huberRegression = huberRegression;
			_env = env;
		}

		public (DateTime Sunrise, DateTime Sunset) GetSolarTimes(DateTime dateTimeLocal, double lat, double lon)
		{
			var solarTimes = new SolarTimes(dateTimeLocal, lat, lon);
			return (solarTimes.Sunrise, solarTimes.Sunset);
		}

		public async Task<DaylightResponseDto?> PredictDaylightAsync(DaylightRequestDto request, string userId)
		{
			var laDir = Path.Combine(_env.WebRootPath, "Users", userId, "LAModels");
			Directory.CreateDirectory(laDir);
			var tempGpxPath = Path.Combine(laDir, $"planned_{Guid.NewGuid():N}.gpx");

			try
			{
				var elevations = _routing.SampleElevation(request.Points);
				WriteGpx(tempGpxPath, request.Points, elevations);

				var predictedSeconds =
					await _huberRegression.PredictTotalTimeSecondsAsync(userId, tempGpxPath);

				if (predictedSeconds is null || predictedSeconds.Value <= 0)
					return null;

				var totalSeconds = predictedSeconds.Value;

				var startLocalTime =
					(request.StartLocalTime?.ToLocalTime()) ?? DateTime.Now;

				var finishLocalTime = startLocalTime.AddSeconds(totalSeconds);

				var first = request.Points[0];
				var (sunrise, sunset) = GetSolarTimes(startLocalTime, first.Lat, first.Lon);

				double percentBeforeNightfall;
				if (sunset <= startLocalTime)
				{
					percentBeforeNightfall = 0;
				}
				else if (sunset >= finishLocalTime)
				{
					percentBeforeNightfall = 100;
				}
				else
				{
					var daylightSeconds = (sunset - startLocalTime).TotalSeconds;
					var frac = daylightSeconds / totalSeconds;
					frac = Math.Clamp(frac, 0.0, 1.0);
					percentBeforeNightfall = frac * 100.0;
				}

				DaylightMarkerDto? sunriseMarker = null;
				DaylightMarkerDto? sunsetMarker = null;

				// Sunrise marker – if sunrise happens during the route
				if (sunrise > startLocalTime && sunrise < finishLocalTime)
				{
					var offset = (sunrise - startLocalTime).TotalSeconds;
					var coord = await _huberRegression.CoordinateAtSecondsAsync(
						userId, tempGpxPath, offset);

					if (coord.HasValue)
					{
						sunriseMarker = new DaylightMarkerDto
						{
							Lat = coord.Value.lat,
							Lon = coord.Value.lon,
							Progress = coord.Value.progress,
							SecondsFromStart = offset
						};
					}
				}

				// Sunset marker – if sunset happens during the route
				if (sunset > startLocalTime && sunset < finishLocalTime)
				{
					var offset = (sunset - startLocalTime).TotalSeconds;
					var coord = await _huberRegression.CoordinateAtSecondsAsync(
						userId, tempGpxPath, offset);

					if (coord.HasValue)
					{
						sunsetMarker = new DaylightMarkerDto
						{
							Lat = coord.Value.lat,
							Lon = coord.Value.lon,
							Progress = coord.Value.progress,
							SecondsFromStart = offset
						};
					}
				}

				return new DaylightResponseDto
				{
					PredictedSeconds = totalSeconds,
					StartTime = startLocalTime,
					FinishTime = finishLocalTime,
					Sunrise = sunrise,
					Sunset = sunset,
					PercentBeforeNightfall = percentBeforeNightfall,
					SunriseMarker = sunriseMarker,
					SunsetMarker = sunsetMarker
				};
			}
			finally
			{
				try { File.Delete(tempGpxPath); } catch { /* ignore */ }
			}
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
	}
}
