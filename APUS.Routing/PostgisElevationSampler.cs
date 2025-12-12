using System;
using Npgsql;

namespace APUS.Routing
{
	public interface IElevationSampler : IDisposable
	{
		// Returns elevation in meters for a given latitude/longitude.
		// Returns null if the point lies outside the DEM extent or if an error occurs.
		float? Sample(double lat, double lon);
	}

	public sealed class PostgisElevationSampler : IElevationSampler
	{
		private readonly NpgsqlDataSource _dataSource;
		private readonly string _tableName;
		private readonly int _rasterSrid;

		public PostgisElevationSampler(
			string connectionString,
			string tableName = "public.eu_dem",
			int rasterSrid = 4258)
		{
			if (string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException(nameof(connectionString));

			_dataSource = NpgsqlDataSource.Create(connectionString);
			_tableName = tableName;
			_rasterSrid = rasterSrid;
		}

		public float? Sample(double lat, double lon)
		{
			const int wgs84 = 4326;

			var sql = $@"
			SELECT ST_Value(
		    rast,
			1,
			ST_Transform(
             ST_SetSRID(ST_Point(@lon, @lat), {wgs84}),
             {_rasterSrid}
			),
			TRUE
			)::float
			FROM   {_tableName}
			WHERE  ST_Intersects(
			rast,
			ST_Transform(
            ST_SetSRID(ST_Point(@lon, @lat), {wgs84}),
            {_rasterSrid}
			)
			)
			LIMIT 1;
			";

			using var conn = _dataSource.OpenConnection();
			using var cmd = new NpgsqlCommand(sql, conn);
			cmd.Parameters.AddWithValue("lon", lon);
			cmd.Parameters.AddWithValue("lat", lat);

			var result = cmd.ExecuteScalar();
			if (result == null || result is DBNull)
				return null;

			return Convert.ToSingle(result);
		}

		public void Dispose() => _dataSource?.Dispose();
	}
}
