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
			double searchDeg = searchRadiusMeters / 111_000.0;

			SnapResult? best = null;
			double bestDist2 = double.PositiveInfinity;

			foreach (var seg in _index.Candidates(lat, lon, searchDeg))
			{
				var from = seg.FromNode;
				var to = seg.ToNode;

				var geom = _graph.GetEdgeGeometry(from, to);
				if (geom != null && geom.Count >= 2)
				{
					if (!ProjectOnPolyline(geom, lat, lon,out double sLat,out double sLon,out double frac,out double d2))
					{
						continue;
					}

					if (d2 >= bestDist2)
						continue;

					if (!_graph.TryGetEdgeCost(from, to, out float edgeCost))
						continue;

					float distFromU = edgeCost * (float)frac;

					bestDist2 = d2;
					best = new SnapResult
					{
						U = from,
						V = to,
						DistFromU = distFromU,
						EdgeLen = edgeCost,
						Point = (sLat, sLon)
					};

					continue;
				}

				// Fallback
				{
					var (uLat, uLon) = _graph.GetNodeLatLon(from);
					var (vLat, vLon) = _graph.GetNodeLatLon(to);

					var (snappedLat, snappedLon, t01) =
						ProjectOnSegment(uLat, uLon, vLat, vLon, lat, lon);
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
			}

			if (best == null)
				throw new InvalidOperationException("Could not snap point to graph (no nearby segments).");

			return best;
		}


		// Project a point (qLat,qLon) onto a polyline geometry.
		// Returns the closest point on the polyline
		private static bool ProjectOnPolyline(
			System.Collections.Generic.IReadOnlyList<(double Lat, double Lon)> geom,
			double qLat,
			double qLon,
			out double bestLat,
			out double bestLon,
			out double frac,
			out double bestDist2)
		{
			bestLat = 0;
			bestLon = 0;
			frac = 0;
			bestDist2 = double.PositiveInfinity;

			if (geom == null || geom.Count < 2)
				return false;

			int n = geom.Count;
			double totalLen = 0.0;

			var segLen = new double[n - 1];
			for (int i = 0; i < n - 1; i++)
			{
				var a = geom[i];
				var b = geom[i + 1];
				double dLat = b.Lat - a.Lat;
				double dLon = b.Lon - a.Lon;
				double len = Math.Sqrt(dLat * dLat + dLon * dLon);
				segLen[i] = len;
				totalLen += len;
			}

			if (totalLen <= 0)
				return false;

			double cumBefore = 0.0;
			double bestCumBefore = 0.0;
			double bestSegLen = 1.0;
			double bestTSeg = 0.0;

			for (int i = 0; i < n - 1; i++)
			{
				var a = geom[i];
				var b = geom[i + 1];

				var (pLat, pLon, t01) = ProjectOnSegment(a.Lat, a.Lon, b.Lat, b.Lon, qLat, qLon);
				if (t01 < 0 || t01 > 1)
				{
					t01 = Math.Max(0, Math.Min(1, t01));
					pLat = t01 == 0 ? a.Lat : b.Lat;
					pLon = t01 == 0 ? a.Lon : b.Lon;
				}

				double dLat = pLat - qLat;
				double dLon = pLon - qLon;
				double d2 = dLat * dLat + dLon * dLon;

				if (d2 < bestDist2)
				{
					bestDist2 = d2;
					bestLat = pLat;
					bestLon = pLon;
					bestCumBefore = cumBefore;
					bestSegLen = segLen[i];
					bestTSeg = t01;
				}

				cumBefore += segLen[i];
			}

			double along = bestCumBefore + bestTSeg * bestSegLen;
			frac = (totalLen > 0) ? (along / totalLen) : 0.0;

			return true;
		}


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
