using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using System.Globalization;
using System.IO;
using System.Xml.Linq;

namespace APUS.Server.Services.Implementations
{
	public class TcxXmlTrackpointLoader : ITrackpointLoader
	{
		private readonly IStorageService _storageService;

		public TcxXmlTrackpointLoader(IStorageService storage)
		{
			_storageService = storage;
		}

		public async Task<List<TrackpointDto>> LoadTrack(MainActivity activity, CancellationToken ct = default)
		{
			string pathToTrackFile = _storageService.ReturnFirstFilePath(activity.Id, activity.UserId);

			XNamespace tcx = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
			XNamespace ext = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";

			await using var fs = new FileStream(pathToTrackFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
			var xdoc = await XDocument.LoadAsync(fs, LoadOptions.None, ct);

			return xdoc
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
					  : null;
				  double? lon = posEl != null
					  ? double.Parse(posEl.Element(tcx + "LongitudeDegrees").Value, CultureInfo.InvariantCulture)
					  : null;

				  // Altitude, Distance, HeartRate
				  double? alt = tp.Element(tcx + "AltitudeMeters") is XElement a ? double.Parse(a.Value, CultureInfo.InvariantCulture) : null;
				  int? hr = tp.Element(tcx + "HeartRateBpm")?
								   .Element(tcx + "Value") is XElement h ? int.Parse(h.Value, CultureInfo.InvariantCulture) : null;

				  // Extensions → <ns3:TPX>
				  var tpx = tp.Element(tcx + "Extensions")?
							  .Element(ext + "TPX");
				  double? speed = tpx?.Element(ext + "Speed") is XElement s ? double.Parse(s.Value, CultureInfo.InvariantCulture) : null;

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
	}
}
