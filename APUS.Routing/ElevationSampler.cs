using MaxRev.Gdal.Core;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace APUS.Routing
{
	public interface IElevationSampler : IDisposable
	{
		// Returns elevation in meters for a given latitude/longitude.
		// Returns null if the point lies outside the DEM extent or if an error occurs.
		float? Sample(double lat, double lon);
	}

	public sealed class GdalElevationSampler : IElevationSampler
	{
		private readonly Dataset _ds;               // GDAL dataset
		private readonly Band _band;                // First raster band 
		private readonly double[] _gt = new double[6]; // GeoTransform: maps geo coords <-> pixel coords
		private readonly CoordinateTransformation _wgs84_to_dem;

		public GdalElevationSampler(string path)
		{
			if (!File.Exists(path))
				throw new FileNotFoundException(path);

			if (!System.IO.File.Exists(path))
			{
				Console.WriteLine("GeoTIFF file not found.");
				return;
			}

			// Open the GeoTIFF DEM in read-only mode
			_ds = Gdal.Open(path, Access.GA_ReadOnly) ?? throw new Exception("GDAL: cannot open DEM");
			_band = _ds.GetRasterBand(1) ?? throw new Exception("GDAL: missing band 1");

			_ds.GetGeoTransform(_gt);

			var src = new SpatialReference(""); src.ImportFromEPSG(4326); 
			var dst = new SpatialReference(_ds.GetProjection());    
			_wgs84_to_dem = new CoordinateTransformation(src, dst);
		}

		public float? Sample(double lat, double lon)
		{
			// Transform geographic coordinates from WGS84 to DEM CRS
			double[] p = new double[] { lon, lat, 0 };
			_wgs84_to_dem.TransformPoint(p);
			double X = p[0], Y = p[1];

			// Convert map coordinates to pixel coordinates using affine transform
			double col = (X - _gt[0]) / _gt[1];
			double row = (Y - _gt[3]) / _gt[5];

			int x0 = (int)Math.Floor(col);
			int y0 = (int)Math.Floor(row);
			int x1 = x0 + 1;
			int y1 = y0 + 1;

			// Check if inside DEM bounds
			if (x0 < 0 || y0 < 0 || x1 >= _ds.RasterXSize || y1 >= _ds.RasterYSize)
				return null;

			// Read a small 2x2 window for bilinear interpolation
			float[] win = new float[4];
			_band.ReadRaster(x0, y0, 2, 2, win, 2, 2, 0, 0);

			float z00 = win[0], z10 = win[1], z01 = win[2], z11 = win[3];

			double fx = col - x0;
			double fy = row - y0;
			double z0 = z00 + (z10 - z00) * fx;
			double z1 = z01 + (z11 - z01) * fx;
			return (float)(z0 + (z1 - z0) * fy);
		}

		public void Dispose()
		{
			// Properly release unmanaged GDAL resources
			_band?.Dispose();
			_ds?.Dispose();
			_wgs84_to_dem?.Dispose();
		}
	}
}
