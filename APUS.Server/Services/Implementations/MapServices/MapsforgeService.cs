using APUS.Server.Controllers.MapController;
using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Services.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Xml.Linq;

namespace APUS.Server.Services.Implementations.MapServices
{
	public sealed class MapsforgeService : IMapsforgeService
	{
		private readonly IHostEnvironment _env;

		// Limits
		private const double MaxWidthDeg = 0.5;
		private const double MaxHeightDeg = 0.5;

		// Tile grid config
		private const double TileSizeDeg = 0.2;
		private const double GlobalMinLat = 45.0;
		private const double GlobalMinLon = 16.0;
		private const double GlobalMaxLat = 49.0;
		private const double GlobalMaxLon = 23.0;

		// Osmosis path
		private readonly string _osmosisExe =
			@"C:\Program Files (x86)\osmosis\bin\osmosis.bat";

		public MapsforgeService(IHostEnvironment env)
		{
			_env = env;
		}

		public (bool ok, string message) ValidateBbox(
			double top,
			double bottom,
			double left,
			double right)
		{
			if (bottom >= top || left >= right)
				return (false, "Invalid bbox (top/bottom/left/right).");

			double width = right - left;
			double height = top - bottom;

			if (width <= 0 || height <= 0)
				return (false, "Invalid bbox dimensions.");

			if (width > MaxWidthDeg || height > MaxHeightDeg)
				return (false, "Requested area is too large (max 0.5° x 0.5°).");

			return (true, "OK");
		}

		public async Task<MapsforgeFileResult?> GenerateMapFromTrackFileAsync(
			string userId,
			string trackFileName)
		{
			// 1) Load coordinates from track file
			var coords = await LoadCoordinatesFromTrackFileAsync(userId, trackFileName);
			if (coords.Count == 0)
				return null;

			double minLat = coords.Min(c => c.Lat);
			double maxLat = coords.Max(c => c.Lat);
			double minLon = coords.Min(c => c.Lon);
			double maxLon = coords.Max(c => c.Lon);

			var (ok, msg) = ValidateBbox(
				top: maxLat,
				bottom: minLat,
				left: minLon,
				right: maxLon);

			if (!ok)
				throw new InvalidOperationException($"Cannot export map: {msg}");

			// 2) Generate map for that bbox
			return await GenerateMapAsync(userId, maxLat, minLat, minLon, maxLon);
		}

		public async Task<MapsforgeFileResult?> GenerateMapAsync(
			string userId,
			double top,
			double bottom,
			double left,
			double right)
		{
			string tilesDir = Path.Combine(_env.ContentRootPath, "tiles0_2");
			if (!Directory.Exists(tilesDir))
				throw new DirectoryNotFoundException($"tiles0_2 not found at {tilesDir}");

			var tiles = GetTilesForBbox(top, bottom, left, right, tilesDir);
			if (tiles.Count == 0)
				return null;

			string userDir = Path.Combine(_env.ContentRootPath, "UserTempMaps", userId);
			Directory.CreateDirectory(userDir);

			string tempMergedPbf = Path.Combine(userDir, "merged_bbox.osm.pbf");
			string outMap = Path.Combine(userDir, "bbox_export.map");

			TryDelete(tempMergedPbf);
			TryDelete(outMap);

			string cmdReadMerge = BuildReadMergeCommand(tiles, top, bottom, left, right, tempMergedPbf);
			int exit1 = await RunOsmosisAsync(cmdReadMerge);
			if (exit1 != 0)
				return null;

			string cmdMapWriter = BuildMapWriterCommand(tempMergedPbf, top, bottom, left, right, outMap);
			int exit2 = await RunOsmosisAsync(cmdMapWriter);
			if (exit2 != 0)
				return null;

			byte[] bytes = await File.ReadAllBytesAsync(outMap);

			string safeUser = string.IsNullOrWhiteSpace(userId)
				? "user"
				: userId.Replace('@', '_').Replace('.', '_');

			string fileName = $"track_{safeUser}_{left:F3}_{bottom:F3}.map";

			return new MapsforgeFileResult
			{
				FileBytes = bytes,
				FileName = fileName
			};
		}

		// ---------- TRACK FILE LOADING (GPX) ----------

		private async Task<List<CoordinateDto>> LoadCoordinatesFromTrackFileAsync(
			string userId,
			string trackFileName)
		{
			// adjust this path to your real storage
			string userTracksDir = Path.Combine(_env.ContentRootPath, "UserTracks", userId);
			string trackPath = Path.Combine(userTracksDir, trackFileName);

			if (!File.Exists(trackPath))
				throw new FileNotFoundException("Track file not found", trackPath);

			await using var stream = File.OpenRead(trackPath);
			var doc = await XDocument.LoadAsync(stream, LoadOptions.None, default);

			// Very simple GPX parsing: <trkpt lat=".." lon="..">
			XNamespace ns = doc.Root?.Name.Namespace ?? XNamespace.None;

			var points = doc
				.Descendants(ns + "trkpt")
				.Select(x =>
				{
					var latAttr = x.Attribute("lat");
					var lonAttr = x.Attribute("lon");
					if (latAttr == null || lonAttr == null)
						return null;

					if (!double.TryParse(latAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
						return null;
					if (!double.TryParse(lonAttr.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
						return null;

					return new CoordinateDto { Lat = lat, Lon = lon };
				})
				.Where(c => c != null)
				.Cast<CoordinateDto>()
				.ToList();

			return points;
		}

		// ---------- TILE SELECTION ----------

		private List<TileInfo> GetTilesForBbox(
			double top,
			double bottom,
			double left,
			double right,
			string tilesDirectory)
		{
			var culture = CultureInfo.InvariantCulture;

			bottom = Math.Max(bottom, GlobalMinLat);
			top = Math.Min(top, GlobalMaxLat);
			left = Math.Max(left, GlobalMinLon);
			right = Math.Min(right, GlobalMaxLon);

			if (bottom >= top || left >= right)
				return new List<TileInfo>();

			const double eps = 1e-9;

			int latIndexMin = (int)Math.Floor((bottom - GlobalMinLat) / TileSizeDeg - eps);
			int latIndexMax = (int)Math.Ceiling((top - GlobalMinLat) / TileSizeDeg + eps) - 1;
			int lonIndexMin = (int)Math.Floor((left - GlobalMinLon) / TileSizeDeg - eps);
			int lonIndexMax = (int)Math.Ceiling((right - GlobalMinLon) / TileSizeDeg + eps) - 1;

			int maxLatIndex = (int)Math.Floor((GlobalMaxLat - GlobalMinLat) / TileSizeDeg);
			int maxLonIndex = (int)Math.Floor((GlobalMaxLon - GlobalMinLon) / TileSizeDeg);

			latIndexMin = Math.Max(latIndexMin, 0);
			lonIndexMin = Math.Max(lonIndexMin, 0);
			latIndexMax = Math.Min(latIndexMax, maxLatIndex);
			lonIndexMax = Math.Min(lonIndexMax, maxLonIndex);

			var tiles = new List<TileInfo>();

			for (int latIndex = latIndexMin; latIndex <= latIndexMax; latIndex++)
			{
				double tileBottom = GlobalMinLat + latIndex * TileSizeDeg;
				double tileTop = tileBottom + TileSizeDeg;

				for (int lonIndex = lonIndexMin; lonIndex <= lonIndexMax; lonIndex++)
				{
					double tileLeft = GlobalMinLon + lonIndex * TileSizeDeg;
					double tileRight = tileLeft + TileSizeDeg;

					if (tileBottom >= top || tileTop <= bottom ||
						tileLeft >= right || tileRight <= left)
						continue;

					string leftStr = tileLeft.ToString("F4", culture);
					string bottomStr = tileBottom.ToString("F4", culture);

					string fileName = $"tile_lon{leftStr}_lat{bottomStr}.osm.pbf";
					string fullPath = Path.Combine(tilesDirectory, fileName);

					if (!File.Exists(fullPath))
						continue;

					tiles.Add(new TileInfo
					{
						BottomLat = tileBottom,
						LeftLon = tileLeft,
						FilePath = fullPath
					});
				}
			}

			return tiles;
		}

		private sealed class TileInfo
		{
			public double BottomLat { get; init; }
			public double LeftLon { get; init; }
			public string FilePath { get; init; } = string.Empty;
		}

		// ---------- OSMOSIS COMMANDS ----------

		private string BuildReadMergeCommand(
			List<TileInfo> tiles,
			double top,
			double bottom,
			double left,
			double right,
			string tempOut)
		{
			var args = new List<string>();
			var culture = CultureInfo.InvariantCulture;

			bool first = true;
			foreach (var tile in tiles)
			{
				args.Add("--rb");
				args.Add($"file=\"{tile.FilePath}\"");

				if (!first)
					args.Add("--merge");

				first = false;
			}

			args.Add("--bb");
			args.Add($"top={top.ToString("F5", culture)}");
			args.Add($"bottom={bottom.ToString("F5", culture)}");
			args.Add($"left={left.ToString("F5", culture)}");
			args.Add($"right={right.ToString("F5", culture)}");
			args.Add("completeWays=yes");
			args.Add("completeRelations=yes");

			args.Add("--wb");
			args.Add($"file=\"{tempOut}\"");

			return string.Join(" ", args);
		}

		private string BuildMapWriterCommand(
			string tempIn,
			double top,
			double bottom,
			double left,
			double right,
			string output)
		{
			var culture = CultureInfo.InvariantCulture;
			var args = new List<string>
			{
				"--rb", $"file=\"{tempIn}\"",
				"--mw", $"file=\"{output}\"",
				"type=ram"
			};

			int threads = Math.Min(Environment.ProcessorCount, 4);
			args.Add($"threads={threads}");

			args.Add(
				$"bbox={bottom.ToString("F5", culture)}," +
				$"{left.ToString("F5", culture)}," +
				$"{top.ToString("F5", culture)}," +
				$"{right.ToString("F5", culture)}");

			return string.Join(" ", args);
		}

		private async Task<int> RunOsmosisAsync(string arguments)
		{
			var psi = new ProcessStartInfo
			{
				FileName = _osmosisExe,
				Arguments = arguments,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using var proc = new Process { StartInfo = psi };

			proc.OutputDataReceived += (_, e) =>
			{
				if (e.Data != null)
					Debug.WriteLine("[Osmosis OUT] " + e.Data);
			};

			proc.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					Debug.WriteLine("[Osmosis ERR] " + e.Data);
			};

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			await proc.WaitForExitAsync();

			return proc.ExitCode;
		}

		private void TryDelete(string path)
		{
			try
			{
				if (File.Exists(path))
					File.Delete(path);
			}
			catch
			{
				// ignore
			}
		}
	}

	public sealed class MapsforgeFileResult
	{
		public required byte[] FileBytes { get; init; }
		public required string FileName { get; init; }
	}
}
