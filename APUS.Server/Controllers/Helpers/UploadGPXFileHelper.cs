using System.Globalization;
using System.IO;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace APUS.Server.Controllers.Helpers
{
	public class UploadGPXFileHelper
	{
		private readonly Stream _stream;
		private List<Trackpoint> Points { get; set; }
		private ImportActivityModel ImportedActivity { get; set; }

		public UploadGPXFileHelper(IFormFile formFile)
		=> _stream = formFile.OpenReadStream();

		public ImportActivityModel ImportActivity()
		{
			Points = ParseGpx(_stream);   // may throw
			ImportedActivity = ComputeStats(Points); // unlikely to throw, but could
			return ImportedActivity;
		}

		private List<Trackpoint> ParseGpx(Stream s)
		{
			var doc = XDocument.Load(s);

			XNamespace ns = "http://www.topografix.com/GPX/1/1";

			// Garmin’s TrackPointExtension namespace
			XNamespace gpxtpx = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1";

			return doc
			  .Descendants(ns + "trkpt")
			  .Select(pt => {
				  // latitude and longitude
				  var lat = double.Parse(pt.Attribute("lat").Value, CultureInfo.InvariantCulture);
				  var lon = double.Parse(pt.Attribute("lon").Value, CultureInfo.InvariantCulture);

				  //safely parse elevation if present
				  var eleEl = pt.Element(ns + "ele");
				  double? ele = eleEl != null
					  ? double.Parse(eleEl.Value, CultureInfo.InvariantCulture)
					  : null;

				  //safely parse time if present
				  var timeEl = pt.Element(ns + "time");
				  DateTime? time = timeEl != null
					  ? DateTime.Parse(timeEl.Value, CultureInfo.InvariantCulture)
					  : null;


				  // navigate into <extensions><gpxtpx:TrackPointExtension>…
				  var ext = pt.Element(ns + "extensions")
							  ?.Element(gpxtpx + "TrackPointExtension");

				  // safely parse hr / cad if present
				  int? hr = ext?.Element(gpxtpx + "hr") is XElement h ? int.Parse(h.Value) : null;
				  int? cad = ext?.Element(gpxtpx + "cad") is XElement c ? int.Parse(c.Value) : null;

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
			  .OrderBy(p => p.Time)
			  .ToList();
		}


		private ImportActivityModel ComputeStats(List<Trackpoint> pts)
		{
			var stats = new ImportActivityModel();

			// 1) only keep points that have both elevation & time
			var valid = pts
			  .Where(p => p.Elevation.HasValue && p.Time.HasValue)
			  .Select(p => new {
				  Lat = p.Latitude,
				  Lon = p.Longitude,
				  Ele = p.Elevation.Value,
				  Time = p.Time.Value
			  })
			  .ToList();

			// 2) accumulate distance & elevation
			for (int i = 1; i < valid.Count; i++)
			{
				var prev = valid[i - 1];
				var curr = valid[i];

				stats.TotalDistanceMeters += Haversine(
					prev.Lat, prev.Lon, curr.Lat, curr.Lon);

				var delta = curr.Ele - prev.Ele;   // now a double
				if (delta > 0) stats.TotalAscentMeters += delta;
				else stats.TotalDescentMeters += -delta;
			}

			// 3) duration is now a non-nullable TimeSpan
			if (valid.Count >= 2)
				stats.Duration = valid.Last().Time - valid.First().Time;

			return stats;
		}


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

	}
}
