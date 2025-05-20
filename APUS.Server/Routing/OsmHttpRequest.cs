using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSMRouting
{
	public class OsmHttpRequest
	{
		private readonly string _osmXml;

		public string GetOsmXml() => _osmXml;

		public OsmHttpRequest(
			string pbfPath,
			double minLat, double minLon,
			double maxLat, double maxLon)
		{
			var tempFile = Path.Combine(@"D:\tmp", "asdasd.osm");

			int minTileLon = (int)Math.Floor(minLon);
			int maxTileLon = (int)Math.Floor(maxLon);
			int minTileLat = (int)Math.Floor(minLat);
			int maxTileLat = (int)Math.Floor(maxLat);

			var sb = new StringBuilder();
			bool first = true;
			for (int lon = minTileLon; lon <= maxTileLon; lon++)
			{
				for (int lat = minTileLat; lat <= maxTileLat; lat++)
				{
					var tileFile = Path.Combine(pbfPath, $"{lon}_{lat}.osm.pbf");
					if (!File.Exists(tileFile)) continue;

					sb.Append("--read-pbf file=\"")
					  .Append(tileFile)
					  .Append("\" ");

					if (!first)
						sb.Append("--merge ");

					first = false;
				}
			}
			if (first)
				throw new FileNotFoundException("No tile found covering the given bbox.", pbfPath);

			// add bbox and filters
			sb.Append($"--bounding-box left={minLon} right={maxLon} top={maxLat} bottom={minLat} completeWays=yes clip=false ");
			sb.Append("--tf accept-ways highway=* --used-node --tf reject-relations ");
			sb.Append($"--write-xml file=\"{tempFile}\"");

			// run osmosis with merged args
			RunOsmosis(sb.ToString());

			_osmXml = File.ReadAllText(tempFile);
		}

		private void RunOsmosis(string arguments)
		{
			var osmosisBat = @"C:\Program Files (x86)\osmosis\bin\osmosis.bat";

			var psi = new ProcessStartInfo
			{
				FileName = osmosisBat,      
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using (var process = new Process { StartInfo = psi })
			{
				process.OutputDataReceived += (sender, e) => {
					if (e.Data != null) Console.WriteLine("[OUT] " + e.Data);
				};
				process.ErrorDataReceived += (sender, e) => {
					if (e.Data != null) Console.Error.WriteLine("[ERR] " + e.Data);
				};

				process.Start();

				process.BeginOutputReadLine();
				process.BeginErrorReadLine();

				process.WaitForExit();

			}
		}


	}
}

