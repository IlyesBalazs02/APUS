using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Domain.Models;
using APUS.Server.Services.Interfaces;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace APUS.Server.Services.Implementations.FileServices
{
	public class TrackpointLoader : ITrackpointLoader
	{
		private readonly IStorageService _storageService;

		public TrackpointLoader(IStorageService storage)
		{
			_storageService = storage;
		}

		public async Task<List<TrackpointDto>> LoadTrack(MainActivity activity, CancellationToken ct = default)
		{
			string pathToTrackFile = _storageService.ReturnFirstFilePath(activity.Id, activity.UserId);

			if (string.IsNullOrWhiteSpace(pathToTrackFile) || !File.Exists(pathToTrackFile))
			{
				// No file -> no trackpoints
				return new List<TrackpointDto>();
			}

			var ext = Path.GetExtension(pathToTrackFile)?.ToLowerInvariant();

			await using var fs = new FileStream(
				pathToTrackFile,
				FileMode.Open,
				FileAccess.Read,
				FileShare.Read,
				4096,
				useAsync: true);

			var xdoc = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

			return ext switch
			{
				".tcx" => ParseTcx(xdoc),
				".gpx" => ParseGpx(xdoc),
				_ => new List<TrackpointDto>() // unsupported type → empty track
			};
		}

		#region TCX parsing (unchanged)

		private List<TrackpointDto> ParseTcx(XDocument xdoc)
		{
			XNamespace tcx = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			XNamespace ext = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";

			return xdoc
				// find all <Trackpoint>
				.Descendants(tcx + "Trackpoint")
				.Select(tp =>
				{
					// Time is mandatory
					var time = DateTime.Parse(
						tp.Element(tcx + "Time")!.Value,
						CultureInfo.InvariantCulture,
						DateTimeStyles.AdjustToUniversal);

					// Position might be missing
					var posEl = tp.Element(tcx + "Position");
					double? lat = posEl != null
						? double.Parse(posEl.Element(tcx + "LatitudeDegrees")!.Value, CultureInfo.InvariantCulture)
						: null;
					double? lon = posEl != null
						? double.Parse(posEl.Element(tcx + "LongitudeDegrees")!.Value, CultureInfo.InvariantCulture)
						: null;

					// Altitude, HeartRate
					double? alt = tp.Element(tcx + "AltitudeMeters") is XElement a
						? double.Parse(a.Value, CultureInfo.InvariantCulture)
						: null;

					int? hr = tp.Element(tcx + "HeartRateBpm")
									?.Element(tcx + "Value") is XElement h
						? int.Parse(h.Value, CultureInfo.InvariantCulture)
						: null;

					// Extensions → <ns3:TPX><Speed>
					var tpx = tp.Element(tcx + "Extensions")
								?.Element(ext + "TPX");
					double? speed = tpx?.Element(ext + "Speed") is XElement s
						? double.Parse(s.Value, CultureInfo.InvariantCulture)
						: null;

					return new TrackpointDto
					{
						Time = time,
						Lat = lat,
						Lon = lon,
						Alt = alt,
						Hr = hr,
						Pace = speed,
					};
				})
				// sort by time
				.OrderBy(p => p.Time)
				.ToList();
		}

		#endregion

		#region GPX parsing (new)

		private List<TrackpointDto> ParseGpx(XDocument xdoc)
		{
			// Standard GPX 1.1 namespace
			XNamespace gpx = "http://www.topografix.com/GPX/1/1";
			// Garmin TrackPoint extensions
			XNamespace gpxtpx = "http://www.garmin.com/xmlschemas/TrackPointExtension/v1";

			return xdoc
				.Descendants(gpx + "trkpt")
				.Select(tp =>
				{
					// lat/lon are on attributes
					var latAttr = tp.Attribute("lat")?.Value;
					var lonAttr = tp.Attribute("lon")?.Value;

					double? lat = null;
					double? lon = null;

					if (double.TryParse(latAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out var latVal))
						lat = latVal;

					if (double.TryParse(lonAttr, NumberStyles.Float, CultureInfo.InvariantCulture, out var lonVal))
						lon = lonVal;

					// elevation (optional)
					double? alt = null;
					var eleEl = tp.Element(gpx + "ele");
					if (eleEl != null && double.TryParse(eleEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var eleVal))
						alt = eleVal;

					// time (optional but usually present)
					DateTime time = default;
					var timeEl = tp.Element(gpx + "time");
					if (timeEl != null)
					{
						// GPX timestamps like 2025-12-07T20:24:53Z
						time = DateTime.Parse(
							timeEl.Value,
							CultureInfo.InvariantCulture,
							DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
					}

					// Extensions: hr / speed may be here
					int? hr = null;
					double? speed = null;

					var extRoot = tp.Element(gpx + "extensions");
					var tpe = extRoot?.Element(gpxtpx + "TrackPointExtension");
					if (tpe != null)
					{
						var hrEl = tpe.Element(gpxtpx + "hr");
						if (hrEl != null && int.TryParse(hrEl.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hrVal))
							hr = hrVal;

						// Some devices store speed as <gpxtpx:Speed> in m/s
						var spEl = tpe.Element(gpxtpx + "Speed");
						if (spEl != null && double.TryParse(spEl.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var spVal))
							speed = spVal;
					}

					return new TrackpointDto
					{
						Time = time,
						Lat = lat,
						Lon = lon,
						Alt = alt,
						Hr = hr,
						Pace = speed
					};
				})
				// filter out points without a valid timestamp if you want, or keep them
				.Where(tp => tp.Time != default)
				.OrderBy(tp => tp.Time)
				.ToList();
		}

		#endregion
	}
}
