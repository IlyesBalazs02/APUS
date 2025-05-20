using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Net.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;

// Alias to disambiguate Path
using SysPath = System.IO.Path;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations
{
	public class CreateOsmMapPng : ICreateOsmMapPng
	{
		private readonly ITrackpointLoader _trackpointLoader;
		private readonly IWebHostEnvironment _env;
		private static readonly HttpClient _http = new HttpClient
		{
			DefaultRequestHeaders = { { "User-Agent", "MyMapApp/1.0" } }
		};

		// Fixed pixel size
		private const int WidthPx = 500;
		private const int HeightPx = 200;
		private const int TileSize = 256;

		public CreateOsmMapPng(ITrackpointLoader trackpointLoader, IWebHostEnvironment env)
		{
			_trackpointLoader = trackpointLoader;
			_env = env;
		}

		public async Task GeneratePng(MainActivity activity)
		{
			var route = await _trackpointLoader.LoadTrack(activity);
			var coords = route.Where(t => t.Lat.HasValue && t.Lon.HasValue)
							  .Select(t => (lat: t.Lat.Value, lon: t.Lon.Value))
							  .ToArray();

			var bbox = GetBbox(coords);
			int zoom = CalculateZoomLevel(bbox.minLat, bbox.minLon, bbox.maxLat, bbox.maxLon);

			byte[] pngBytes = await GenerateFixedSizeMap(coords, zoom);

			string outPath = SysPath.Combine(_env.WebRootPath, "Users", activity.UserId, "Activities", activity.Id, "ActivityTrackImage.png");
			await File.WriteAllBytesAsync(outPath, pngBytes);
		}

		private static (double minLat, double minLon, double maxLat, double maxLon) GetBbox((double lat, double lon)[] pts)
		{
			var lats = pts.Select(p => p.lat);
			var lons = pts.Select(p => p.lon);
			return (lats.Min(), lons.Min(), lats.Max(), lons.Max());
		}

		private static async Task<byte[]> GenerateFixedSizeMap((double lat, double lon)[] pts, int zoom)
		{
			// Convert route to global pixel coordinates
			var globalPixels = pts.Select(p => LatLonToPixel(p.lat, p.lon, zoom)).ToArray();
			// Compute pixel bbox
			double minX = globalPixels.Min(p => p.px);
			double maxX = globalPixels.Max(p => p.px);
			double minY = globalPixels.Min(p => p.py);
			double maxY = globalPixels.Max(p => p.py);
			// Center of route
			double centerX = (minX + maxX) / 2;
			double centerY = (minY + maxY) / 2;

			// Top-left of viewport
			double vpX = centerX - WidthPx / 2.0;
			double vpY = centerY - HeightPx / 2.0;

			// Which tiles needed
			int xTileMin = (int)Math.Floor(vpX / TileSize);
			int yTileMin = (int)Math.Floor(vpY / TileSize);
			int xTileMax = (int)Math.Ceiling((vpX + WidthPx) / TileSize);
			int yTileMax = (int)Math.Ceiling((vpY + HeightPx) / TileSize);

			int tilesX = xTileMax - xTileMin + 1;
			int tilesY = yTileMax - yTileMin + 1;

			// Stitch full tile image
			using var full = new Image<Rgba32>(tilesX * TileSize, tilesY * TileSize);
			for (int x = xTileMin; x <= xTileMax; x++)
				for (int y = yTileMin; y <= yTileMax; y++)
				{
					string url = $"https://tile.openstreetmap.org/{zoom}/{x}/{y}.png";
					var data = await _http.GetByteArrayAsync(url);
					using var tile = Image.Load<Rgba32>(data);
					full.Mutate(ctx => ctx.DrawImage(tile, new Point((x - xTileMin) * TileSize, (y - yTileMin) * TileSize), 1f));
				}

			// Crop viewport
			int cropX = (int)Math.Round(vpX - xTileMin * TileSize);
			int cropY = (int)Math.Round(vpY - yTileMin * TileSize);
			full.Mutate(ctx => ctx.Crop(new Rectangle(cropX, cropY, WidthPx, HeightPx)));

			// Draw route adjusted to viewport
			var ptsRel = globalPixels.Select(p => new PointF(
				(float)(p.px - vpX),
				(float)(p.py - vpY)
			)).ToArray();

			full.Mutate(ctx => ctx.DrawLine(Color.Red, 4f, ptsRel));

			using var ms = new MemoryStream();
			await full.SaveAsPngAsync(ms);
			return ms.ToArray();
		}

		private static (double px, double py) LatLonToPixel(double lat, double lon, int zoom)
		{
			double scale = TileSize * Math.Pow(2, zoom);
			double px = (lon + 180) / 360 * scale;
			double sinLat = Math.Sin(lat * Math.PI / 180);
			double py = (0.5 - Math.Log((1 + sinLat) / (1 - sinLat)) / (4 * Math.PI)) * scale;
			return (px, py);
		}

		private static int CalculateZoomLevel(double minLat, double minLon, double maxLat, double maxLon)
		{
			const int MaxZoom = 18;
			const int MinZoom = 1;
			for (int z = MaxZoom; z >= MinZoom; z--)
			{
				// Fit pixel span to viewport
				var p0 = LatLonToPixel(minLat, minLon, z);
				var p1 = LatLonToPixel(maxLat, maxLon, z);
				if (Math.Abs(p1.px - p0.px) <= WidthPx && Math.Abs(p1.py - p0.py) <= HeightPx)
					return z;
			}
			return MinZoom;
		}
	}
}
