public sealed class SegmentIndex
{
	// Reference to a piece of an edge: Geometry[segIdx] -> Geometry[segIdx+1]
	public readonly struct SegRef
	{
		public readonly int FromNode;   // owner of the edge in Adj[FromNode]
		public readonly int EdgeIdx;    // index in Adj[FromNode]
		public readonly int SegIdx;     // segment inside Edge.Geometry
		public readonly double MinLat, MinLon, MaxLat, MaxLon; // bbox

		public SegRef(int from, int ei, int si, double minLa, double minLo, double maxLa, double maxLo)
		{ FromNode = from; EdgeIdx = ei; SegIdx = si; MinLat = minLa; MinLon = minLo; MaxLat = maxLa; MaxLon = maxLo; }
	}

	private readonly Dictionary<(int iy, int ix), List<SegRef>> _grid = new();
	private readonly double _cellDeg; // ~0.01 => ~1km cells

	public SegmentIndex(RoadGraph g, double cellDegrees = 0.01)
	{
		_cellDeg = cellDegrees;

		for (int u = 0; u < g.Adj.Count; u++)
		{
			var edges = g.Adj[u];
			for (int ei = 0; ei < edges.Count; ei++)
			{
				var e = edges[ei];
				if (e.Geometry.Count < 2) continue;

				for (int si = 0; si < e.Geometry.Count - 1; si++)
				{
					var a = e.Geometry[si];
					var b = e.Geometry[si + 1];

					double minLat = Math.Min(a.Lat, b.Lat);
					double maxLat = Math.Max(a.Lat, b.Lat);
					double minLon = Math.Min(a.Lon, b.Lon);
					double maxLon = Math.Max(a.Lon, b.Lon);

					var seg = new SegRef(u, ei, si, minLat, minLon, maxLat, maxLon);

					var (iy0, ix0) = Key(minLat, minLon);
					var (iy1, ix1) = Key(maxLat, maxLon);
					for (int iy = iy0; iy <= iy1; iy++)
						for (int ix = ix0; ix <= ix1; ix++)
						{
							var key = (iy, ix);
							if (!_grid.TryGetValue(key, out var list))
								_grid[key] = list = new List<SegRef>();
							list.Add(seg);
						}
				}
			}
		}
	}

	private (int iy, int ix) Key(double lat, double lon)
		=> ((int)Math.Floor(lat / _cellDeg), (int)Math.Floor(lon / _cellDeg));

	// Nearby candidates from cell + neighbors
	public IEnumerable<SegRef> Candidates(double lat, double lon)
	{
		var (iy, ix) = Key(lat, lon);
		for (int dy = -1; dy <= 1; dy++)
			for (int dx = -1; dx <= 1; dx++)
			{
				if (_grid.TryGetValue((iy + dy, ix + dx), out var list))
					for (int i = 0; i < list.Count; i++)
						yield return list[i];
			}
	}
}

public sealed class Snapper
{
	private readonly RoadGraph _g;
	private readonly SegmentIndex _index;

	public Snapper(RoadGraph g, SegmentIndex index)
	{ _g = g; _index = index; }

	public readonly struct SnapResultTmp
	{
		public readonly int FromNode;     // Adj owner
		public readonly int EdgeIdx;      // which edge in Adj[FromNode]
		public readonly int SegIdx;       // segment inside edge geometry
		public readonly double T;         // 0..1 along this segment
		public readonly double Lat, Lon;  // snapped coordinates
		public readonly double DistMeters;// distance from click to segment (meters)
		public readonly float LenFromStart; // meters from edge start-node to snap
		public readonly float LenToEnd;     // meters from snap to edge end-node

		public SnapResultTmp(int fromNode, int edgeIdx, int segIdx, double t, double lat, double lon, double dist,
						  float lenFromStart, float lenToEnd)
		{ FromNode = fromNode; EdgeIdx = edgeIdx; SegIdx = segIdx; T = t; Lat = lat; Lon = lon; DistMeters = dist; LenFromStart = lenFromStart; LenToEnd = lenToEnd; }
	}


	public SnapResult Snap(double lat, double lon)
	{
		double bestD = double.MaxValue;
		SnapResult best = null!;  // or default; we'll always set it before return

		double cosLat = Math.Cos(lat * Math.PI / 180.0);
		double dxScale = 111_320.0 * cosLat;
		double dyScale = 110_540.0;

		foreach (var s in _index.Candidates(lat, lon))
		{
			var e = _g.Adj[s.FromNode][s.EdgeIdx];
			if (e.CumulativeLength == null || e.Geometry.Count < 2)
				continue;

			var a = e.Geometry[s.SegIdx];
			var b = e.Geometry[s.SegIdx + 1];

			// local meters
			double ax = (a.Lon - lon) * dxScale, ay = (a.Lat - lat) * dyScale;
			double bx = (b.Lon - lon) * dxScale, by = (b.Lat - lat) * dyScale;
			double vx = bx - ax, vy = by - ay;
			double v2 = vx * vx + vy * vy;
			if (v2 < 1e-6) continue;

			double t = Math.Clamp((-(ax * vx + ay * vy)) / v2, 0.0, 1.0);
			double px = ax + t * vx;
			double py = ay + t * vy;
			double d = Math.Sqrt(px * px + py * py);
			if (d >= bestD) continue;

			// back to lat/lon
			double snapLon = lon + (px / dxScale);
			double snapLat = lat + (py / dyScale);

			// edge cumulative length
			var cum = e.CumulativeLength!;
			float lenToSegmentStart = cum[s.SegIdx];
			float segLen = (float)HaversineMeters(a.Lat, a.Lon, b.Lat, b.Lon);
			float lenAlongSegment = (float)(t * segLen);
			float lenFromStart = lenToSegmentStart + lenAlongSegment;
			float edgeLen = cum[^1];

			bestD = d;
			best = new SnapResult
			{
				U = s.FromNode,                 // start node of the edge
				V = e.To,                       // end node of the edge
				DistFromU = lenFromStart,       // meters from U to snap
				EdgeLen = edgeLen,              // full edge length
				Point = (snapLat, snapLon)      // snapped coordinate
			};
		}

		return best;
	}


	// Local copy to avoid changing existing access modifiers
	private static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
	{
		const double R = 6371000.0;
		double dLat = (lat2 - lat1) * Math.PI / 180.0;
		double dLon = (lon2 - lon1) * Math.PI / 180.0;
		double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
				   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
				   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return 2 * R * Math.Asin(Math.Min(1, Math.Sqrt(a)));
	}
}

public sealed class SnapResult
{
	// Directed edge the point snapped to: U -> V
	public int U { get; init; }
	public int V { get; init; }

	// Distance from U along edge (meters) and total edge length (meters)
	public float DistFromU { get; init; }
	public float EdgeLen { get; init; }

	// The exact snapped coordinate on the edge
	public (double Lat, double Lon) Point { get; init; }
}
