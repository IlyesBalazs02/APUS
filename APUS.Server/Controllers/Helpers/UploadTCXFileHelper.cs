using System.Globalization;
using System.Net.NetworkInformation;
using System.Xml.Linq;

namespace APUS.Server.Controllers.Helpers
{
	public class UploadTCXFileHelper
	{
		private readonly Stream _stream;
		private List<TcxTrackPoint> Points { get; set; }
		private List<LapSummary> Laps { get; set; }
		private ImportActivityModel ImportedActivity { get; set; }

		public UploadTCXFileHelper(IFormFile formFile)
		=> _stream = formFile.OpenReadStream();

		public ImportActivityModel ImportActivity()
		{
			//parse trackpoints
			Points = ParseTcxTrackpoints(_stream);

			_stream.Seek(0, SeekOrigin.Begin);

			//parse lap extensions
			Laps = ParseLapSummaries(_stream);

			//Compute additional information about the activity
			ImportedActivity = ComputeAdditionalStats(Laps);

			//Check if the uploaded activity contains coordinates or it's just a simple activity
			ImportedActivity.HasGpsTrack = ContainsGPSTrack(Points);

			return ImportedActivity;
		}

		private List<TcxTrackPoint> ParseTcxTrackpoints(Stream stream)
		{
			var doc = XDocument.Load(stream);

			// TCX core namespace:
			XNamespace tcx = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			// Garmin Activity Extension (for Speed, RunCadence, Watts):
			XNamespace ext = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";

			//parse trackpoints
			return doc
			  // find all <Trackpoint>
			  .Descendants(tcx + "Trackpoint")
			  .Select(tp =>
			  {
				  // Time is mandatory
				  var time = DateTime.Parse(
					  tp.Element(tcx + "Time").Value,
					  CultureInfo.InvariantCulture,
					  DateTimeStyles.AdjustToUniversal);

				  // Position might be missing
				  var posEl = tp.Element(tcx + "Position");
				  double? lat = posEl != null
					  ? double.Parse(posEl.Element(tcx + "LatitudeDegrees").Value, CultureInfo.InvariantCulture)
					  : (double?)null;
				  double? lon = posEl != null
					  ? double.Parse(posEl.Element(tcx + "LongitudeDegrees").Value, CultureInfo.InvariantCulture)
					  : (double?)null;

				  // Altitude, Distance, HeartRate
				  double? alt = tp.Element(tcx + "AltitudeMeters") is XElement a ? double.Parse(a.Value, CultureInfo.InvariantCulture) : (double?)null;
				  double? dist = tp.Element(tcx + "DistanceMeters") is XElement d ? double.Parse(d.Value, CultureInfo.InvariantCulture) : (double?)null;
				  int? hr = tp.Element(tcx + "HeartRateBpm")?
								   .Element(tcx + "Value") is XElement h ? int.Parse(h.Value, CultureInfo.InvariantCulture) : (int?)null;

				  // Extensions → <ns3:TPX>
				  var tpx = tp.Element(tcx + "Extensions")?
							  .Element(ext + "TPX");
				  double? speed = tpx?.Element(ext + "Speed") is XElement s ? double.Parse(s.Value, CultureInfo.InvariantCulture) : (double?)null;
				  int? runCadence = tpx?.Element(ext + "RunCadence") is XElement r ? int.Parse(r.Value, CultureInfo.InvariantCulture) : (int?)null;
				  int? watts = tpx?.Element(ext + "Watts") is XElement w ? int.Parse(w.Value, CultureInfo.InvariantCulture) : (int?)null;

				  return new TcxTrackPoint
				  {
					  Time = time,
					  Latitude = lat,
					  Longitude = lon,
					  Altitude = alt,
					  Distance = dist,
					  HeartRate = hr,
					  Speed = speed,
					  RunCadence = runCadence,
					  Watts = watts
				  };
			  })
			  // sort by time
			  .OrderBy(p => p.Time)
			  .ToList();
		}

		private List<LapSummary> ParseLapSummaries(Stream stream)
		{
			var doc = XDocument.Load(stream);

			// TCX core namespace:
			XNamespace tcx = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			// Garmin Activity Extension (for Speed, RunCadence, Watts):
			XNamespace ext = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";

			//pars lap summaries
			return doc
			  .Descendants(tcx + "Lap")
			  .Select(lap => {
				  var start = DateTime.Parse(
					  lap.Attribute("StartTime").Value,
					  CultureInfo.InvariantCulture,
					  DateTimeStyles.AdjustToUniversal);

				  double totalTime = double.Parse(
					  lap.Element(tcx + "TotalTimeSeconds").Value,
					  CultureInfo.InvariantCulture);

				  double distance = double.Parse(
					  lap.Element(tcx + "DistanceMeters").Value,
					  CultureInfo.InvariantCulture);

				  int calories = int.Parse(
					  lap.Element(tcx + "Calories").Value,
					  CultureInfo.InvariantCulture);

				  int? avgHr = lap
					.Element(tcx + "AverageHeartRateBpm")
					?.Element(tcx + "Value") is XElement ahr
					  ? int.Parse(ahr.Value, CultureInfo.InvariantCulture)
					  : (int?)null;

				  int? maxHr = lap
					.Element(tcx + "MaximumHeartRateBpm")
					?.Element(tcx + "Value") is XElement mhr
					  ? int.Parse(mhr.Value, CultureInfo.InvariantCulture)
					  : (int?)null;

				  //ns3:LX> block:
				  var lx = lap.Element(tcx + "Extensions")
							  ?.Element(ext + "LX");

				  double? avgSpeed = lx?.Element(ext + "AvgSpeed") is XElement s ? double.Parse(s.Value, CultureInfo.InvariantCulture) : (double?)null;
				  int? avgCad = lx?.Element(ext + "AvgRunCadence") is XElement c ? int.Parse(c.Value, CultureInfo.InvariantCulture) : (int?)null;
				  int? maxCad = lx?.Element(ext + "MaxRunCadence") is XElement m ? int.Parse(m.Value, CultureInfo.InvariantCulture) : (int?)null;


				  return new LapSummary
				  {
					  StartTime = start,
					  TotalTimeSeconds = totalTime,
					  DistanceMeters = distance,
					  Calories = calories,
					  AverageHeartRate = avgHr,
					  MaximumHeartRate = maxHr,
					  AvgSpeed = avgSpeed,
					  AvgRunCadence = avgCad,
					  MaxRunCadence = maxCad
				  };
			  })
			  .ToList();
		}

		private ImportActivityModel ComputeAdditionalStats(List<LapSummary> laps)
		{
			double totalTime = laps.Sum(l => l.TotalTimeSeconds ?? 0);

			double totalDistanceMeters = laps.Sum(l => l.DistanceMeters ?? 0);

			double totalDistanceKm = Math.Ceiling((totalDistanceMeters / 1000.0) * 100) / 100.0;

			var avgHrDouble = laps
				.Where(l => l.AverageHeartRate.HasValue)
				.Select(l => l.AverageHeartRate.Value)
				.Average();

			double avgSpeedTmp = laps.Average(l => l.AvgSpeed ?? 0);



			//Calc the totalAscent and TotalDescent
			var elevationPoints = Points
				   .Where(p => p.Altitude.HasValue)
				   .Select(p => p.Altitude.Value)
				   .ToList();

			double ascentTmp = 0;
			double descentTmp = 0;

			for (int i = 1; i < elevationPoints.Count; i++)
			{
				var delta = elevationPoints[i] - elevationPoints[i - 1];
				if (delta > 0)
					ascentTmp += delta;
				else
					descentTmp += -delta;
			}

			var stats = new ImportActivityModel
			{
				StartTime = laps.First().StartTime,
				TotalTimeSeconds = totalTime,
				Duration = TimeSpan.FromSeconds(Math.Floor(totalTime)),
				TotalDistanceMeters = totalDistanceMeters,
				TotalDistanceKm = totalDistanceKm,
				AvgPace = avgSpeedTmp,
				TotalCalories = laps.Sum(l => l.Calories ?? 0),
				AverageHeartRate = (int)avgHrDouble,
				MaximumHeartRate = laps
					.Where(l => l.MaximumHeartRate.HasValue)
					.Select(l => l.MaximumHeartRate.Value)
					.Max(),
				TotalAscentMeters = ascentTmp,
				TotalDescentMeters = descentTmp
			};

			return stats;
		}

		private bool ContainsGPSTrack(List<TcxTrackPoint> points)
		{
			return points.Any(p => p.Latitude.HasValue && p.Longitude.HasValue);
		}

		private class TcxTrackPoint
		{
			public DateTime Time { get; set; }
			public double? Latitude { get; set; }
			public double? Longitude { get; set; }
			public double? Altitude { get; set; }
			public double? Distance { get; set; }
			public int? HeartRate { get; set; }
			public double? Speed { get; set; }
			public int? RunCadence { get; set; }
			public int? Watts { get; set; }
		}

		private class LapSummary
		{
			public DateTime StartTime { get; set; }
			public double? TotalTimeSeconds { get; set; }
			public double? DistanceMeters { get; set; }
			public int? Calories { get; set; }
			public int? AverageHeartRate { get; set; }
			public int? MaximumHeartRate { get; set; }

			//for the LX extension at the end of the laps
			public double? AvgSpeed { get; set; }  // in m/s
			public int? AvgRunCadence { get; set; }
			public int? MaxRunCadence { get; set; }

		}
	}
}
