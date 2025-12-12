using APUS.Server.Domain.DTOs.Routing;
using APUS.Routing;
using APUS.Server.Services.Interfaces;

namespace APUS.Server.Services.Implementations.MapServices
{
	public sealed class RoutingService : IRoutingService
	{
		private readonly TiledRoadGraph _graph;
		private readonly Snapper _snapper;
		private readonly IElevationSampler _elevationSampler;

		private readonly object _elevLock = new();

		public RoutingService(
			TiledRoadGraph graph,
			Snapper snapper,
			IElevationSampler elevationSampler)
		{
			_graph = graph ?? throw new ArgumentNullException(nameof(graph));
			_snapper = snapper ?? throw new ArgumentNullException(nameof(snapper));
			_elevationSampler = elevationSampler ?? throw new ArgumentNullException(nameof(elevationSampler));
		}

		public SnapResponseDto SnapToRoad(double lat, double lon)
		{
			var snap = _snapper.Snap(lat, lon);
			var (sLat, sLon) = snap.Point;

			return new SnapResponseDto
			{
				Lat = sLat,
				Lon = sLon
			};
		}

		public IReadOnlyList<RouteCoordinateDto> RouteBetweenCoords(
			double fromLat,
			double fromLon,
			double toLat,
			double toLon)
		{
			var snapA = _snapper.Snap(fromLat, fromLon);
			var snapB = _snapper.Snap(toLat, toLon);

			var nodePath = VirtualEndpointsGraph.RouteBetweenSnaps(_graph, snapA, snapB);
			if (nodePath is null || nodePath.Count == 0)
				throw new InvalidOperationException("No route found between the given coordinates.");

			// Rebuild the full polyline between the two snapped points
			var poly = BuildRouteGeometry(_graph, snapA, snapB, nodePath);

			var coords = new List<RouteCoordinateDto>(poly.Count);
			foreach (var (lat, lon) in poly)
			{
				coords.Add(new RouteCoordinateDto
				{
					Lat = lat,
					Lon = lon
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
				lock (_elevLock)
				{
					result[i] = _elevationSampler.Sample(p.Lat, p.Lon);
				}
			}
			return result;
		}


		// Build the full polyline (lat/lon) between snapped endpoints
		private static List<(double Lat, double Lon)> BuildRouteGeometry(
			TiledRoadGraph graph,
			SnapResult A,
			SnapResult B,
			IReadOnlyList<NodeKey> path)
		{
			if (path.Count < 2)
				throw new ArgumentException("Path must contain at least two nodes (S and T).", nameof(path));

			var S = new NodeKey(new TileId(-1), 0);
			var T = new NodeKey(new TileId(-2), 0);

			var poly = new List<(double Lat, double Lon)>();

			poly.Add(A.Point);

			for (int i = 0; i < path.Count - 1; i++)
			{
				var from = path[i];
				var to = path[i + 1];

				IReadOnlyList<(double Lat, double Lon)> segmentGeom;

				if (from == S)
				{
					if (to.Equals(A.U))
					{
						segmentGeom = graph.GetPartialEdgeGeometry(A.U, A.V, A.DistFromU, 0f);
					}
					else if (to.Equals(A.V))
					{
						segmentGeom = graph.GetPartialEdgeGeometry(A.U, A.V, A.DistFromU, A.EdgeLen);
					}
					else
					{
						// Should not happen; skip
						continue;
					}
				}
				else if (to == T)
				{
					if (from.Equals(B.U))
					{
						segmentGeom = graph.GetPartialEdgeGeometry(B.U, B.V, 0f, B.DistFromU);
					}
					else if (from.Equals(B.V))
					{
						segmentGeom = graph.GetPartialEdgeGeometry(B.U, B.V, B.EdgeLen, B.DistFromU);
					}
					else
					{
						continue;
					}

					AppendSegment(poly, segmentGeom);
					break;
				}
				else
				{
					var geom = graph.GetEdgeGeometry(from, to);
					if (geom == null || geom.Count == 0)
					{
						var pFrom = graph.GetNodeLatLon(from);
						var pTo = graph.GetNodeLatLon(to);
						segmentGeom = new List<(double Lat, double Lon)>
						{
							pFrom,
							pTo
						};
					}
					else
					{
						segmentGeom = geom;
					}
				}

				AppendSegment(poly, segmentGeom);
			}

			if (poly.Count == 0 || poly[^1] != B.Point)
				poly.Add(B.Point);

			return poly;
		}

		private static void AppendSegment(
			List<(double Lat, double Lon)> poly,
			IReadOnlyList<(double Lat, double Lon)> segment)
		{
			if (segment == null || segment.Count == 0)
				return;

			int startIdx = poly.Count > 0 ? 1 : 0;
			for (int i = startIdx; i < segment.Count; i++)
				poly.Add(segment[i]);
		}
	}
}

