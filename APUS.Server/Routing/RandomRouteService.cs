
using System.Text.Json;
using APUS.Server.Routing;
using OSMGraphCreater;


namespace APUS.Server.Routing
{
	public interface IRandomRouteService
	{
		/// <summary>
		/// Creates a random route between two random nodes and returns GeoJSON.
		/// </summary>
		string CreateRandomRouteGeoJson();
	}

	public sealed class RandomRouteService : IRandomRouteService, IDisposable
	{
		private readonly PagedRoadGraph _graph;

		public RandomRouteService(PagedRoadGraph graph)
		{
			_graph = graph;
		}

		public string CreateRandomRouteGeoJson()
		{
			// Your two Hungarian coordinates
			double fromLat = 47.490257, fromLon = 18.999738;
			double toLat = 47.482205, toLon = 18.352889;

			if (_graph.NodeCount == 0)
				throw new InvalidOperationException("Graph has no nodes.");

			// 1) find nearest nodes in the graph
			int start = AStarRouter.NearestNode(_graph, fromLat, fromLon);
			int goal = AStarRouter.NearestNode(_graph, toLat, toLon);

			// 2) run A*
			var path = AStarRouter.ShortestPath(_graph, start, goal);
			if (path.Count == 0)
				throw new InvalidOperationException("No path found between the snapped nodes.");

			// 3) collect coordinates along the path
			var coordinates = new List<double[]>();
			foreach (var nodeId in path)
			{
				var (lat, lon) = _graph.GetNodeLatLon(nodeId);
				coordinates.Add(new[] { lon, lat }); // [lon, lat] for GeoJSON
			}

			// If you literally just want a list of coords, you could return
			// JsonSerializer.Serialize(coordinates) instead. For now, GeoJSON:

			var feature = new
			{
				type = "Feature",
				properties = new { },
				geometry = new
				{
					type = "LineString",
					coordinates = coordinates
				}
			};

			var featureCollection = new
			{
				type = "FeatureCollection",
				features = new[] { feature }
			};
			var asd = JsonSerializer.Serialize(featureCollection);

			return asd;
		}

		public void Dispose() => _graph.Dispose();
	}
}
