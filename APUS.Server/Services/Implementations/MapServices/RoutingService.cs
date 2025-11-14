using APUS.Server.Domain.DTOs.Routing;
using APUS.Server.Routing;

namespace APUS.Server.Services.Implementations.MapServices
{
	public sealed class RoutingService : IRoutingService
	{
		private readonly PagedRoadGraph _graph;

		public RoutingService(PagedRoadGraph graph)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
		}

		public SnapResponseDto SnapToRoad(double lat, double lon)
		{
			if (_graph.NodeCount == 0)
				throw new InvalidOperationException("Graph has no nodes.");

			var nodeId = AStarRouter.NearestNode(_graph, lat, lon);
			if (nodeId < 0)
				throw new InvalidOperationException("No nearest node found.");

			var (nLat, nLon) = _graph.GetNodeLatLon(nodeId);

			return new SnapResponseDto
			{
				NodeId = nodeId,
				Lat = nLat,
				Lon = nLon
			};
		}

		public IReadOnlyList<RouteCoordinateDto> RouteBetweenCoords(
			double fromLat, double fromLon,
			double toLat, double toLon)
		{
			if (_graph.NodeCount == 0)
				throw new InvalidOperationException("Graph has no nodes.");

			// 1) snap endpoints to nearest nodes
			int start = AStarRouter.NearestNode(_graph, fromLat, fromLon);
			int goal = AStarRouter.NearestNode(_graph, toLat, toLon);

			if (start < 0 || goal < 0)
				throw new InvalidOperationException("Could not snap endpoints to graph.");

			// 2) run A*
			var path = AStarRouter.ShortestPath(_graph, start, goal);
			if (path.Count == 0)
				throw new InvalidOperationException("No path found between the snapped nodes.");

			// 3) collect coordinates along the path
			var coords = new List<RouteCoordinateDto>(path.Count);
			foreach (var nodeId in path)
			{
				var (lat, lon) = _graph.GetNodeLatLon(nodeId);
				coords.Add(new RouteCoordinateDto { Lat = lat, Lon = lon });
			}

			return coords;
		}
	}
}
