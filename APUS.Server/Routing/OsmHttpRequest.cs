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
		private string _osmXml;

		public string GetOsmXml() => _osmXml;

		public OsmHttpRequest(
			string pbfPath,
			double minLat, double minLon,
			double maxLat, double maxLon)
		{
			var tempFile = Path.Combine(@"D:\tmp", "asdasd2.osm");

			int minTileLon = (int)Math.Floor(minLon);
			int maxTileLon = (int)Math.Floor(maxLon);
			int minTileLat = (int)Math.Floor(minLat);
			int maxTileLat = (int)Math.Floor(maxLat);

			var sb = new StringBuilder();

			for (int lon = minTileLon; lon <= maxTileLon; lon++)
			{
				for (int lat = minTileLat; lat <= maxTileLat; lat++)
				{
					var tileFile = Path.Combine(pbfPath, $"{lon}_{lat}.osm.pbf");
					if (!File.Exists(tileFile))
						continue;

					// just read each tile, no merging
					sb.Append("--read-pbf file=\"")
					  .Append(tileFile)
					  .Append("\" ");
				}
			}

			/*if (first)
				throw new FileNotFoundException("No tile found covering the given bbox.", pbfPath);*/

			if (sb.Length == 0)
				throw new InvalidOperationException($"No PBF tiles found in '{pbfPath}' for that bbox.");


			// add bbox and filters
			// correct: no “clip” parameter (or rename if you need it)
			sb.Append($"--bounding-box left={minLon} right={maxLon} top={maxLat} bottom={minLat} completeWays=yes ");
			sb.Append("--tf accept-ways highway=* --used-node --tf reject-relations ");
			//sb.Append($"--write-xml file=\"{tempFile}\"");
			sb.Append("--write-xml file=-");

			// run osmosis with merged args
			RunOsmosis(sb.ToString());
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
				process.Start();                                   
				_osmXml = process.StandardOutput.ReadToEnd();      
				var errors = process.StandardError.ReadToEnd();   
				process.WaitForExit();

				if (process.ExitCode != 0)
					throw new InvalidOperationException(
						$"Osmosis failed (exit {process.ExitCode}):\n{errors}"
					);

			}
		}


	}
}

