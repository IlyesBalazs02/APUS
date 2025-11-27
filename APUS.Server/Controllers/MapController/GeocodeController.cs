using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace APUS.Server.Controllers.MapController
{
	[ApiController]
	[Route("api/[controller]")]
	public class GeocodeController : ControllerBase
	{
		private readonly IDbConnection _geoDb;

		public GeocodeController(IDbConnection geoDb)
		{
			_geoDb = geoDb;
		}

		public class PlaceSearchResult
		{
			public long Id { get; set; }
			public string Name { get; set; }
			public string Class { get; set; }
			public string Type { get; set; }
			public double Lat { get; set; }
			public double Lon { get; set; }
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<PlaceSearchResult>>> Search(
			[FromQuery] string q,
			[FromQuery] double? lat,
			[FromQuery] double? lon)
		{
			if (string.IsNullOrWhiteSpace(q))
				return BadRequest("Query cannot be empty.");

			q = q.Trim();
			Console.WriteLine($"q:{q}   lat:{lat}   lon:{lon}");

			string sql;
			object args;

			if (q.Length < 3)
			{
				sql = @"
SELECT
    id, name, class, type, importance,
    ST_Y(geom) AS lat,
    ST_X(geom) AS lon
FROM public.places
WHERE LOWER(name) LIKE LOWER(@q) || '%'
ORDER BY importance DESC
LIMIT 10;
";
				args = new { q };
			}
			else
			{
				sql = @"
SELECT
    id,
    name,
    class,
    type,
    importance,
    ST_Y(geom) AS lat,
    ST_X(geom) AS lon,
    ts_rank(search_vector, plainto_tsquery('simple', @q)) AS score,
    ST_Distance(
        geom,
        ST_SetSRID(ST_MakePoint(@lon, @lat), 4326)
    ) AS dist
FROM public.places
WHERE search_vector @@ plainto_tsquery('simple', @q)
ORDER BY score DESC, importance DESC, dist ASC
LIMIT 10;
";
				args = new { q, lat, lon };
			}

			var results = await _geoDb.QueryAsync<PlaceSearchResult>(sql, args);
			return Ok(results);
		}
	}
}
