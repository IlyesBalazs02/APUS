using System;

namespace APUS.Routing
{
	public sealed class SnapResult
	{
		// Directed edge the point snapped to U -> V in tiled graph
		public NodeKey U { get; init; }
		public NodeKey V { get; init; }

		// Distance from U along edge
		public float DistFromU { get; init; }
		public float EdgeLen { get; init; }

		// The exact snapped coordinate on the edge
		public (double Lat, double Lon) Point { get; init; }
	}

	// Snaps arbitrary coordinates to the nearest road segment in the tiled graph.
	public sealed class Snapper
	{
		private readonly TiledRoadGraph _graph;
		private readonly SegmentIndex _index;

		public Snapper(TiledRoadGraph graph, SegmentIndex index)
		{
			_graph = graph;
			_index = index;
		}

		/// Build a SegmentIndex that covers all tiles in the graph.
		public static SegmentIndex BuildGlobalIndex(TiledRoadGraph graph, double cellDegrees = 0.01)
		{
			var index = new SegmentIndex(cellDegrees);

			foreach (var tileId in graph.GetAllTileIds())
			{
				int nodeCount = graph.GetNodeCount(tileId);
				for (int local = 0; local < nodeCount; local++)
				{
					var from = new NodeKey(tileId, local);
					var (uLat, uLon) = graph.GetNodeLatLon(from);

					foreach (var (neighbor, _) in graph.GetNeighbors(from))
					{
						var (vLat, vLon) = graph.GetNodeLatLon(neighbor);
						index.AddSegment(from, neighbor, uLat, uLon, vLat, vLon);
					}
				}
			}

			return index;
		}

		// Snap a coordinate to the nearest point on any nearby road segment.
		public SnapResult Snap(double lat, double lon, double searchRadiusMeters = 200.0)
		{
			// Rough conversion: 1 deg ~ 111km
			double searchDeg = searchRadiusMeters / 111_000.0;

			SnapResult? best = null;
			double bestDist2 = double.PositiveInfinity;

			foreach (var seg in _index.Candidates(lat, lon, searchDeg))
			{
				var from = seg.FromNode;
				var to = seg.ToNode;

				var (uLat, uLon) = _graph.GetNodeLatLon(from);
				var (vLat, vLon) = _graph.GetNodeLatLon(to);

				var (snappedLat, snappedLon, t01) = ProjectOnSegment(uLat, uLon, vLat, vLon, lat, lon);
				if (t01 < 0 || t01 > 1)
					continue;

				double dLat = snappedLat - lat;
				double dLon = snappedLon - lon;
				double d2 = dLat * dLat + dLon * dLon;
				if (d2 >= bestDist2)
					continue;

				if (!_graph.TryGetEdgeCost(from, to, out float edgeLen))
					continue;

				float distFromU = edgeLen * (float)t01;

				bestDist2 = d2;
				best = new SnapResult
				{
					U = from,
					V = to,
					DistFromU = distFromU,
					EdgeLen = edgeLen,
					Point = (snappedLat, snappedLon)
				};
			}

			if (best == null)
				throw new InvalidOperationException("Could not snap point to graph (no nearby segments).");

			return best;
		}

		// Orthogonal projection of point P onto segment AB in (lat, lon) space.
		// Returns (projectionLat, projectionLon, t in [0,1]).
		private static (double Lat, double Lon, double T01) ProjectOnSegment(
			double latA, double lonA,
			double latB, double lonB,
			double latP, double lonP)
		{
			double vx = lonB - lonA;
			double vy = latB - latA;
			double wx = lonP - lonA;
			double wy = latP - latA;

			double c1 = vx * wx + vy * wy;
			double c2 = vx * vx + vy * vy;
			if (c2 <= 0)
				return (latA, lonA, 0);

			double t = c1 / c2;
			double lonProj = lonA + t * vx;
			double latProj = latA + t * vy;

			return (latProj, lonProj, t);
		}
	}
}
