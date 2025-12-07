using APUS.Server.Domain.DTOs.Feature.Activity;
using APUS.Server.Services.Interfaces;
using System.Globalization;
using System.Xml.Linq;

namespace APUS.Server.Services.Implementations.FileServices
{
	public class GPXFileService : IGPXFileService
	{
		private List<Trackpoint> Points { get; set; } = new();
		private ImportActivityModel ImportedActivity { get; set; }

		public ImportActivityModel ImportActivity(MemoryStream gpxStream)
		{
			if (gpxStream == null)
				throw new ArgumentNullException(nameof(gpxStream));

			if (gpxStream.CanSeek)
				gpxStream.Position = 0;

			Points = ParseGpx(gpxStream);
			ImportedActivity = ComputeStats(Points);

			// If there is at least one valid coordinate, treat as GPS activity
			ImportedActivity.HasGpsTrack = Points.Any(p =>
				!double.IsNaN(p.Latitude) &&
				!double.IsNaN(p.Longitude));

			return ImportedActivity;
		}

		#region Parsing

		private List<Trackpoint> ParseGpx(MemoryStream stream)
		{
			var doc = XDocument.Load(stream);

			XNamespace gpx = "http://www.topografix.com/GPX/1/1";
			XNamespace gpxtpx = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1";

			return doc
				.Descendants(gpx + "trkpt")
				.Select(pt =>
				{
					// lat / lon
					if (!double.TryParse(pt.Attribute("lat")?.Value,
							NumberStyles.Float,
							CultureInfo.InvariantCulture,
							out var lat))
						return null;

					if (!double.TryParse(pt.Attribute("lon")?.Value,
							NumberStyles.Float,
							CultureInfo.InvariantCulture,
							out var lon))
						return null;

					// elevation (optional)
					double? ele = null;
					var eleEl = pt.Element(gpx + "ele");
					if (eleEl != null &&
						double.TryParse(eleEl.Value,
							NumberStyles.Float,
							CultureInfo.InvariantCulture,
							out var eleVal))
					{
						ele = eleVal;
					}

					// time (optional but usually present)
					DateTime? time = null;
					var timeEl = pt.Element(gpx + "time");
					if (timeEl != null &&
						DateTime.TryParse(
							timeEl.Value,
							CultureInfo.InvariantCulture,
							DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
							out var tVal))
					{
						time = tVal;
					}

					// extensions: HR, cadence, speed (garmin)
					int? hr = null;
					int? cad = null;

					var extRoot = pt.Element(gpx + "extensions");
					var tpe = extRoot?.Element(gpxtpx + "TrackPointExtension");
					if (tpe != null)
					{
						var hrEl = tpe.Element(gpxtpx + "hr");
						if (hrEl != null &&
							int.TryParse(hrEl.Value,
								NumberStyles.Integer,
								CultureInfo.InvariantCulture,
								out var hrVal))
						{
							hr = hrVal;
						}

						var cadEl = tpe.Element(gpxtpx + "cad");
						if (cadEl != null &&
							int.TryParse(cadEl.Value,
								NumberStyles.Integer,
								CultureInfo.InvariantCulture,
								out var cadVal))
						{
							cad = cadVal;
						}
					}

					return new Trackpoint
					{
						Latitude = lat,
						Longitude = lon,
						Elevation = ele,
						Time = time,
						HeartRate = hr,
						Cadence = cad
					};
				})
				.Where(p => p != null)
				.OrderBy(p => p!.Time ?? DateTime.MinValue)
				.Select(p => p!)
				.ToList();
		}

		#endregion

		#region Stats

		private ImportActivityModel ComputeStats(List<Trackpoint> pts)
		{
			var model = new ImportActivityModel();

			if (pts == null || pts.Count == 0)
				return model;

			// Time-ordered points
			var timeOrdered = pts
				.Where(p => p.Time.HasValue)
				.OrderBy(p => p.Time!.Value)
				.ToList();

			double totalTimeSeconds = 0;
			if (timeOrdered.Count >= 2)
			{
				var start = timeOrdered.First().Time!.Value;
				var finish = timeOrdered.Last().Time!.Value;

				totalTimeSeconds = (finish - start).TotalSeconds;

				model.StartTime = start;
				model.FinishTimeUtc = finish;
				model.TotalTimeSeconds = totalTimeSeconds;
				model.Duration = TimeSpan.FromSeconds(Math.Floor(totalTimeSeconds));
			}

			double totalDistanceMeters = 0;
			double ascent = 0;
			double descent = 0;

			for (int i = 1; i < timeOrdered.Count; i++)
			{
				var prev = timeOrdered[i - 1];
				var curr = timeOrdered[i];

				totalDistanceMeters += Haversine(
					prev.Latitude, prev.Longitude,
					curr.Latitude, curr.Longitude);

				if (prev.Elevation.HasValue && curr.Elevation.HasValue)
				{
					var delta = curr.Elevation.Value - prev.Elevation.Value;
					if (delta > 0) ascent += delta;
					else descent += -delta;
				}
			}

			// --- rounding to 2 decimals ---
			var totalDistanceKm = totalDistanceMeters / 1000.0;
			totalDistanceKm = Math.Round(totalDistanceKm, 2);
			ascent = Math.Round(ascent, 2);
			descent = Math.Round(descent, 2);

			model.TotalDistanceMeters = totalDistanceMeters;
			model.TotalDistanceKm = totalDistanceKm;
			model.TotalAscentMeters = ascent;
			model.TotalDescentMeters = descent;

			// HR stats
			var hrList = pts
				.Where(p => p.HeartRate.HasValue)
				.Select(p => p.HeartRate!.Value)
				.ToList();

			if (hrList.Any())
			{
				model.AverageHeartRate = (int)hrList.Average();
				model.MaximumHeartRate = hrList.Max();
			}

			// Avg speed (m/s) → rounded to 2 decimals
			if (totalTimeSeconds > 0 && totalDistanceMeters > 0)
			{
				var avgSpeed = totalDistanceMeters / totalTimeSeconds; // m/s
				model.AvgPace = Math.Round(avgSpeed, 2);
			}

			// Simple calorie estimate based on distance
			// ~60 kcal per km (you can tune this constant).
			if (totalDistanceKm > 0)
			{
				model.TotalCalories = (int)Math.Round(totalDistanceKm * 60.0);
			}
			else
			{
				model.TotalCalories = 0;
			}

			return model;
		}

		#endregion

		#region Helpers

		private class Trackpoint
		{
			public double Latitude { get; set; }
			public double Longitude { get; set; }
			public double? Elevation { get; set; }
			public DateTime? Time { get; set; }
			public int? HeartRate { get; set; }
			public int? Cadence { get; set; }
		}

		private double Haversine(double lat1, double lon1, double lat2, double lon2)
		{
			const double R = 6_371_000; // metres
			double dLat = ToRad(lat2 - lat1);
			double dLon = ToRad(lon2 - lon1);
			double a = Math.Pow(Math.Sin(dLat / 2), 2) +
					   Math.Cos(ToRad(lat1)) *
					   Math.Cos(ToRad(lat2)) *
					   Math.Pow(Math.Sin(dLon / 2), 2);
			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return R * c;
		}

		private double ToRad(double deg) => deg * (Math.PI / 180);

		#endregion
	}
}
