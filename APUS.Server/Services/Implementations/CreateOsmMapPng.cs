using APUS.Server.DTOs;
using APUS.Server.Models;
using APUS.Server.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System.Numerics;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;

namespace APUS.Server.Services.Implementations
{
	public class CreateOsmMapPng : ICreateOsmMapPng
	{
		ITrackpointLoader _trackpointLoader;
		IWebHostEnvironment _env;
		public CreateOsmMapPng(ITrackpointLoader trackpointLoader, IWebHostEnvironment env)
		{
			_trackpointLoader = trackpointLoader;
			_env = env;
		}
		public async Task GeneratePng(MainActivity activity)
		{

			var route = await _trackpointLoader.LoadTrack(activity);

			var bbox = GetBboxParams(route);

			var routeTuples = route
			  .Where(t => t.Lat.HasValue && t.Lon.HasValue)
			  .Select(t => (lat: t.Lat.Value, lon: t.Lon.Value))
			  .ToArray();

			byte[] pngBytes = await GenerateMapPngAsync(routeTuples,
				bbox[0], bbox[1], bbox[2], bbox[3], zoom: 13);


			string outPath = Path.Combine(_env.WebRootPath, "Users", activity.UserId, "Activities", activity.Id, "ActivityTrackImage.png");

			await File.WriteAllBytesAsync(outPath, pngBytes);
			Console.WriteLine($"Map saved to {outPath}");
		}

		private static readonly HttpClient _http = new HttpClient
		{
			DefaultRequestHeaders = { { "User-Agent", "MyMapApp/1.0" } }
		};

		private static async Task<byte[]> GenerateMapPngAsync(
		(double lat, double lon)[] route,
		double minLat, double minLon,
		double maxLat, double maxLon,
		int zoom = 13)
		{
			// 1. Figure tile X/Y ranges
			(double xtMin, double ytMax) = LatLonToTile(minLat, minLon, zoom);
			(double xtMax, double ytMin) = LatLonToTile(maxLat, maxLon, zoom);

			int x0 = (int)Math.Floor(xtMin);
			int x1 = (int)Math.Floor(xtMax);
			int y0 = (int)Math.Floor(ytMin);
			int y1 = (int)Math.Floor(ytMax);

			int tileCountX = x1 - x0 + 1;
			int tileCountY = y1 - y0 + 1;
			const int tileSize = 256;

			// 2. Create canvas
			using var image = new Image<Rgba32>(tileCountX * tileSize, tileCountY * tileSize);

			// 3. Download & paste each tile
			for (int tx = x0; tx <= x1; tx++)
			{
				for (int ty = y0; ty <= y1; ty++)
				{
					var url = $"https://tile.openstreetmap.org/{zoom}/{tx}/{ty}.png";
					var data = await _http.GetByteArrayAsync(url);
					using var tileImg = Image.Load<Rgba32>(data);

					int destX = (tx - x0) * tileSize;
					int destY = (ty - y0) * tileSize;
					image.Mutate(ctx => ctx.DrawImage(tileImg, new Point(destX, destY), 1f));
				}
			}

			// 4. Convert route points → pixel positions
			Vector2[] pxPoints = new Vector2[route.Length];
			float scale = tileSize * (float)Math.Pow(2, zoom);
			for (int i = 0; i < route.Length; i++)
			{
				var (lat, lon) = route[i];
				// world pixel coords
				float wx = (float)((lon + 180) / 360 * scale);
				float wy = (float)((1 - Math.Log(Math.Tan(lat * Math.PI / 180) +
						   1 / Math.Cos(lat * Math.PI / 180)) / Math.PI) / 2 * scale);
				// offset by top-left tile
				pxPoints[i] = new Vector2(
					wx - x0 * tileSize,
					wy - y0 * tileSize
				);
			}

			var pointFs = pxPoints
		.Select(v => new PointF(v.X, v.Y))
		.ToArray();

			// Draw a 4-px red line through them:
			image.Mutate(ctx =>
				ctx.DrawLine(
					Color.Red,   //  <-- uses SixLabors.ImageSharp.Color
					4f,          //  thickness
					pointFs      //  your pixel coords
				)
			);

			// 6. Export to PNG bytes
			using var ms = new MemoryStream();
			await image.SaveAsPngAsync(ms);
			return ms.ToArray();
		}

		// Helper: lat/lon → fractional tile X/Y
		private static (double xt, double yt) LatLonToTile(double lat, double lon, int zoom)
		{
			double n = Math.Pow(2, zoom);
			double xt = (lon + 180.0) / 360.0 * n;
			double latRad = lat * Math.PI / 180.0;
			double yt = (1.0 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2.0 * n;
			return (xt, yt);
		}

		private double[] GetBboxParams(List<TrackpointDto> route)
		{
			if (route == null || !route.Any())
				throw new ArgumentException("Route must contain at least one point.", nameof(route));

			// force non-null here
			double minLat = route.Where(t => t.Lat.HasValue).Min(p => p.Lat!.Value);
			double maxLat = route.Where(t => t.Lat.HasValue).Max(p => p.Lat!.Value);
			double minLon = route.Where(t => t.Lon.HasValue).Min(p => p.Lon!.Value);
			double maxLon = route.Where(t => t.Lon.HasValue).Max(p => p.Lon!.Value);

			//  padding
			const double paddingMeters = 100;

			double latPad = paddingMeters / 111_300.0;
			double avgLatRad = ((minLat + maxLat) / 2) * Math.PI / 180.0;
			double lonPad = paddingMeters / (111_300.0 * Math.Cos(avgLatRad));

			minLat -= latPad;
			maxLat += latPad;
			minLon -= lonPad;
			maxLon += lonPad;

			return new[] { minLat, minLon, maxLat, maxLon };
		}

	}
}
