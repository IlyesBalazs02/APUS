using APUS.Server.Domain.DTOs.Routing;
using APUS.Routing;

namespace APUS.Server.Services.Implementations.MapServices
{
	public sealed class RoutingService : IRoutingService
	{
		private readonly PagedRoadGraph _graph;
		private readonly APUS.Routing.IElevationSampler _elevationSampler;

		public RoutingService(PagedRoadGraph graph, APUS.Routing.IElevationSampler elevationSampler)
		{
			_graph = graph;
			_elevationSampler = elevationSampler;
		}

		public SnapResponseDto SnapToRoad(double lat, double lon)
		{
			if (_graph.NodeCount == 0)
				throw new InvalidOperationException("Graph has no nodes.");

			// SEGMENT-BASED SNAP (uses edge geometry + cumulative lengths)
			var snap = PagedRoadGraph.SnapToGraph(_graph, (lat, lon));
			var (sLat, sLon) = snap.Point;

			return new SnapResponseDto
			{
				NodeId = snap.U, // not really used by frontend, but fine
				Lat = sLat,
				Lon = sLon
			};
		}

		public IReadOnlyList<RouteCoordinateDto> RouteBetweenCoords(
			double fromLat, double fromLon,
			double toLat, double toLon)
		{
			if (_graph.NodeCount == 0)
				throw new InvalidOperationException("Graph has no nodes.");

			// *** KEY POINT ***
			// This function:
			//  - snaps both endpoints onto *segments* (partial edges)
			//  - builds a VirtualEndpointsGraph overlay (S/T nodes)
			//  - runs A* on the overlay
			//  - reconstructs the full polyline using edge geometry (segmented)
			var poly = PagedRoadGraph.GetRouteGeometryBetweenCoords(
				_graph,
				(fromLat, fromLon),
				(toLat, toLon));

			var coords = new List<RouteCoordinateDto>(poly.Count);
			for (int i = 0; i < poly.Count; i++)
			{
				coords.Add(new RouteCoordinateDto
				{
					Lat = poly[i].Lat,
					Lon = poly[i].Lon
				});
			}

			return coords;
		}

		public IReadOnlyList<float?> SampleElevation(IReadOnlyList<RouteCoordinateDto> points)
		{
			if (points == null || points.Count == 0)
				return Array.Empty<float?>();

			var result = new float?[points.Count];
			for (int i = 0; i < points.Count; i++)
			{
				var p = points[i];
				result[i] = _elevationSampler.Sample(p.Lat, p.Lon);
			}
			return result;
		}
	}
}

