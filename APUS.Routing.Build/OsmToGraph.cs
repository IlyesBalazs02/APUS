using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OSMGraphCreater;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;

public sealed class RoadGraph : IReadOnlyGraph
{
	public int NodeCount => Nodes.Count;

	public (double Lat, double Lon) GetNodeLatLon(int id)
		=> (Nodes[id].Lat, Nodes[id].Lon);

	public IReadOnlyList<LightEdge> GetAdj(int u)
	{
		var list = Adj[u];
		if (list.Count == 0) return Array.Empty<LightEdge>();
		var outv = new LightEdge[list.Count];
		for (int i = 0; i < list.Count; i++)
			outv[i] = new LightEdge(list[i].To, list[i].Weight);
		return outv;
	}


	// Represents an intersection or road endpoint
	public sealed class Node
	{
		public int Idx;      // Internal numeric ID (array index)
		public long OsmId;   // Original OSM node ID (unique globally)
		public double Lat, Lon; // Latitude/Longitud
	}

	// Represents a directional road segment (edge) between two nodes
	public sealed class Edge
	{
		public int To;                             // Destination node index
		public float Weight;                       // Travel cost in meters and elevation change
		public List<(double Lat, double Lon)> Geometry = new(); // List of all intermediate coordinates

		// CumLen[i] = meters from Geometry[0] to Geometry[i]
		// Helps compute partial distances on an edge
		public float[]? CumulativeLength;

		public long? OsmWayId;
	}

	// Graph storage
	public List<Node> Nodes { get; } = new();                    // All nodes in the graph
	public List<List<Edge>> Adj { get; } = new();                // Adjacency list: edges from each node
	public Dictionary<long, int> OsmIdToIdx { get; } = new();    // Map OSM node ID -> internal index

	// Adds a node if it doesn't exist, otherwise returns its existing index
	public int GetOrAddNode(long osmId, double lat, double lon)
	{
		if (OsmIdToIdx.TryGetValue(osmId, out var idx)) return idx;
		idx = Nodes.Count;
		OsmIdToIdx[osmId] = idx;
		Nodes.Add(new Node { Idx = idx, OsmId = osmId, Lat = lat, Lon = lon });
		Adj.Add(new List<Edge>());
		return idx;
	}

	// Adds an edge (connection) between two nodes with specified weight and geometry
	public void AddEdge(int u, int v, float w, List<(double Lat, double Lon)> geometry, float[] cumLen, long? wayId)
		=> Adj[u].Add(new Edge { To = v, Weight = w, Geometry = geometry, CumulativeLength = cumLen, OsmWayId = wayId });
}

// ElevationCost — adjusts distances based on terrain slope
public static class ElevationCost
{
	public const double K_UP = 3.0;   // uphill penalty
	public const double K_DOWN = 1.0; // gentle downhill bonus
	public const double G_FREE = 0.05;// 5% downhill = "free"
	public const double K_STEEP = 6.0;// steep downhill penalty

	// Compute a multiplier for a road segment based on DEM elevation changes.
	public static double ComputeMultiplier(List<(double Lat, double Lon)> geom, IElevationSampler? dem)
	{
		if (dem == null || geom.Count < 2) return 1.0;

		double sumLen = 0, sumWeighted = 0;
		float? hPrev = dem.Sample(geom[0].Lat, geom[0].Lon);

		for (int i = 0; i < geom.Count - 1; i++)
		{
			var a = geom[i]; var b = geom[i + 1];
			double len = HaversineMeters(a.Lat, a.Lon, b.Lat, b.Lon);
			if (len < 0.5) continue; // skip very short fragments

			float? hA = hPrev ?? dem.Sample(a.Lat, a.Lon);
			float? hB = dem.Sample(b.Lat, b.Lon);
			hPrev = hB;

			double mult = 1.0;
			if (hA != null && hB != null)
			{
				double dh = hB.Value - hA.Value;
				double grade = dh / Math.Max(len, 1.0); // slope = delta h / distance

				if (grade >= 0)
					mult = 1.0 + K_UP * grade;             // uphill -> slower
				else
				{
					double d = -grade; // downhill magnitude
					if (d <= G_FREE) mult = 1.0 - K_DOWN * d;        // gentle downhill -> bonus
					else mult = 1.0 + K_STEEP * (d - G_FREE);        // steep downhill -> penalty
				}
			}

			sumWeighted += mult * len;
			sumLen += len;
		}

		if (sumLen <= 0) return 1.0;
		return Math.Clamp(sumWeighted / sumLen, 0.5, 3.0);
	}

	// Utility: compute 2D distance in meters between coordinates
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

// GraphBuilder — creates the graph from OpenStreetMap data
public static class GraphBuilder
{
	// road filter: accept only OSM ways that are roads.
	// don't store the highway type anywhere — this is only to avoid buildings/areas/etc.
	private static readonly HashSet<string> RoadHighways = new(StringComparer.OrdinalIgnoreCase)
{
	"motorway","trunk","primary","secondary","tertiary","unclassified","residential",
	"motorway_link","trunk_link","primary_link","secondary_link","tertiary_link","service","living_street"
};

	private static bool IsRoutableWay(Way w)
	{
		if (w.Tags == null) return false;
		if (!w.Tags.TryGetValue("highway", out var hw)) return false;
		if (!RoadHighways.Contains(hw)) return false;

		return true;
	}

	// Main builder: converts OSM .pbf into a RoadGraph, using elevation data
	public static RoadGraph BuildFromPbf(string pbfPath, IElevationSampler? dem = null)
	{
		using var fs = File.OpenRead(pbfPath);
		var source = new PBFOsmStreamSource(fs);
		var all = source.ToList(); // loads OSM data (nodes + ways)

		// Extract node coordinates
		var nodes = all.OfType<Node>().ToDictionary(n => n.Id!.Value, n => (Lat: n.Latitude!.Value, Lon: n.Longitude!.Value));
		var ways = all.OfType<Way>().Where(IsRoutableWay).ToList();

		// Count how many roads reference each node → detect intersections
		var refCount = new Dictionary<long, int>(capacity: 1_000_000);
		foreach (var w in ways)
		{
			if (w.Nodes == null || w.Nodes.Length < 2) continue;
			var uniq = new HashSet<long>(w.Nodes);
			foreach (var id in uniq)
				if (!refCount.TryAdd(id, 1)) refCount[id]++;
		}

		var g = new RoadGraph();

		// Process every OSM way
		foreach (var w in ways)
		{
			if (w.Nodes == null || w.Nodes.Length < 2) continue;

			// Keep only nodes that exist (avoid missing coordinate data)
			var refs = w.Nodes.Where(n => nodes.ContainsKey(n)).ToArray();
			if (refs.Length < 2) continue;

			// Define what makes a node "important" (intersection or road end)
			bool IsImportantIndex(int idx)
			{
				if (idx == 0 || idx == refs.Length - 1) return true;
				return refCount.TryGetValue(refs[idx], out var c) && c >= 2;
			}

			int segStartIdx = 0;
			while (segStartIdx < refs.Length - 1)
			{
				// Find the next "important" node after the current one
				int segEndIdx = segStartIdx + 1;
				while (segEndIdx < refs.Length - 1 && !IsImportantIndex(segEndIdx))
					segEndIdx++;

				var startId = refs[segStartIdx];
				var endId = refs[segEndIdx];

				// Build the list of coordinates for this segment (+ cumulative lengths)
				var geom = new List<(double, double)>();
				var cumulative = new List<float>(); // cumulative length along geom
				double totalMeters = 0;
				(double Lat, double Lon) prev = nodes[startId];
				geom.Add((prev.Lat, prev.Lon));
				cumulative.Add(0f);

				for (int k = segStartIdx + 1; k <= segEndIdx; k++)
				{
					var curr = nodes[refs[k]];
					totalMeters += HaversineMeters(prev.Lat, prev.Lon, curr.Lat, curr.Lon);
					geom.Add((curr.Lat, curr.Lon));
					cumulative.Add((float)totalMeters);
					prev = curr;
				}

				// Compute the terrain multiplier using DEM
				double mult = ElevationCost.ComputeMultiplier(
					geom.Select(p => (Lat: p.Item1, Lon: p.Item2)).ToList(), dem);
				float weight = (float)(totalMeters * mult); // Final edge weight

				// Create graph nodes if missing
				var (sLat, sLon) = nodes[startId];
				var (tLat, tLon) = nodes[endId];
				int u = g.GetOrAddNode(startId, sLat, sLon);
				int v = g.GetOrAddNode(endId, tLat, tLon);

				// Add edge for u -> v
				g.AddEdge(u, v, weight,
					geom.Select(p => (Lat: p.Item1, Lon: p.Item2)).ToList(),
					cumulative.ToArray(),
					w.Id);

				// Add reverse edge for v -> u (two-way, as requested)
				var revGeom = new List<(double, double)>(geom);
				revGeom.Reverse();

				// rebuild cumulative for reversed geometry
				var revCum = new float[revGeom.Count];
				double acc = 0;
				revCum[0] = 0;
				for (int i = 1; i < revGeom.Count; i++)
				{
					var a = revGeom[i - 1]; var b = revGeom[i];
					acc += HaversineMeters(a.Item1, a.Item2, b.Item1, b.Item2);
					revCum[i] = (float)acc;
				}

				g.AddEdge(v, u, weight,
					revGeom.Select(p => (Lat: p.Item1, Lon: p.Item2)).ToList(),
					revCum,
					w.Id);

				segStartIdx = segEndIdx;
			}
		}
		return g;
	}

	// Distance between coordinates (used throughout)
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

public static class AStarRouter
{
	private sealed class OpenItem : IComparable<OpenItem>
	{
		public int Node;
		public float F;
		public int CompareTo(OpenItem other) => F.CompareTo(other.F);
	}

	// Returns nodes path + also lets you reconstruct edges used
	public static List<int> ShortestPath(IReadOnlyGraph g, int start, int goal)
	{
		int n = g.NodeCount;
		var gScore = Enumerable.Repeat(float.PositiveInfinity, n).ToArray();
		var fScore = Enumerable.Repeat(float.PositiveInfinity, n).ToArray();
		var cameFrom = Enumerable.Repeat(-1, n).ToArray();

		var open = new PriorityQueue<int, float>();
		gScore[start] = 0;
		fScore[start] = Heuristic(g, start, goal);
		open.Enqueue(start, fScore[start]);

		var inOpen = new bool[n];
		inOpen[start] = true;

		while (open.Count > 0)
		{
			var current = open.Dequeue();
			if (current == goal) return ReconstructPath(cameFrom, current);

			var adj = g.GetAdj(current);
			for (int i = 0; i < adj.Count; i++)
			{
				var e = adj[i];
				float tentative = gScore[current] + e.Weight;
				if (tentative < gScore[e.To])
				{
					cameFrom[e.To] = current;
					gScore[e.To] = tentative;
					fScore[e.To] = tentative + Heuristic(g, e.To, goal);
					if (!inOpen[e.To])
					{
						open.Enqueue(e.To, fScore[e.To]);
						inOpen[e.To] = true;
					}
				}
			}
		}
		return new List<int>();
	}

	private static List<int> ReconstructPath(int[] cameFrom, int cur)
	{
		var path = new List<int> { cur };
		while (cameFrom[cur] != -1) { cur = cameFrom[cur]; path.Add(cur); }
		path.Reverse(); return path;
	}

	private static float Heuristic(IReadOnlyGraph g, int u, int v)
	{
		var a = g.GetNodeLatLon(u); var b = g.GetNodeLatLon(v);
		return (float)Math.Sqrt((a.Lat - b.Lat) * (a.Lat - b.Lat) + (a.Lon - b.Lon) * (a.Lon - b.Lon)) * 111_000f;
	}

	public static int NearestNodeLinear(RoadGraph g, double lat, double lon)
	{
		int best = -1;
		double bestD2 = double.MaxValue;
		for (int i = 0; i < g.Nodes.Count; i++)
		{
			double dLat = g.Nodes[i].Lat - lat;
			double dLon = g.Nodes[i].Lon - lon;
			double d2 = dLat * dLat + dLon * dLon;
			if (d2 < bestD2) { bestD2 = d2; best = i; }
		}
		return best;
	}

	public static string ExportRouteNodesGeoJson(RoadGraph g, List<int> path)
	{
		// Build GeoJSON features for each node in the path
		var features = new List<object>();

		foreach (var idx in path)
		{
			var node = g.Nodes[idx];
			features.Add(new
			{
				type = "Feature",
				properties = new { },
				geometry = new
				{
					type = "Point",
					coordinates = new[] { node.Lon, node.Lat } // GeoJSON = [lon, lat]
				}
			});
		}

		var geojson = new
		{
			type = "FeatureCollection",
			features = features
		};

		return JsonSerializer.Serialize(geojson);
	}
}

// SegmentIndex — spatial grid over edge segments for snapping
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

// Snapper — finds the nearest point on the network to a click
public sealed class Snapper
{
	private readonly RoadGraph _g;
	private readonly SegmentIndex _index;

	public Snapper(RoadGraph g, SegmentIndex index)
	{ _g = g; _index = index; }

	// Result of snapping a lat/lon to the nearest segment
	public readonly struct SnapResult
	{
		public readonly int FromNode;     // Adj owner
		public readonly int EdgeIdx;      // which edge in Adj[FromNode]
		public readonly int SegIdx;       // segment inside edge geometry
		public readonly double T;         // 0..1 along this segment
		public readonly double Lat, Lon;  // snapped coordinates
		public readonly double DistMeters;// distance from click to segment (meters)
		public readonly float LenFromStart; // meters from edge start-node to snap
		public readonly float LenToEnd;     // meters from snap to edge end-node

		public SnapResult(int fromNode, int edgeIdx, int segIdx, double t, double lat, double lon, double dist,
						  float lenFromStart, float lenToEnd)
		{ FromNode = fromNode; EdgeIdx = edgeIdx; SegIdx = segIdx; T = t; Lat = lat; Lon = lon; DistMeters = dist; LenFromStart = lenFromStart; LenToEnd = lenToEnd; }
	}

	public SnapResult Snap(double lat, double lon)
	{
		double bestD = double.MaxValue;
		SnapResult best = default;

		// local linearization for small distances around the query
		double cosLat = Math.Cos(lat * Math.PI / 180.0);
		double dxScale = 111_320.0 * cosLat; // meters per degree lon
		double dyScale = 110_540.0;          // meters per degree lat

		foreach (var s in _index.Candidates(lat, lon))
		{
			var e = _g.Adj[s.FromNode][s.EdgeIdx];
			var a = e.Geometry[s.SegIdx];
			var b = e.Geometry[s.SegIdx + 1];

			// convert to local meters
			double ax = (a.Lon - lon) * dxScale, ay = (a.Lat - lat) * dyScale;
			double bx = (b.Lon - lon) * dxScale, by = (b.Lat - lat) * dyScale;
			double vx = bx - ax, vy = by - ay;
			double v2 = vx * vx + vy * vy;
			if (v2 < 1e-6) continue;

			// projection of origin (0,0) onto AB, clamped to [0,1]
			double t = Math.Clamp((-(ax * vx + ay * vy)) / v2, 0.0, 1.0);
			double px = ax + t * vx; double py = ay + t * vy;
			double d = Math.Sqrt(px * px + py * py);
			if (d >= bestD) continue;

			// back to lat/lon
			double snapLon = lon + (px / dxScale);
			double snapLat = lat + (py / dyScale);

			// partial distances along the whole edge using CumLen
			var cum = e.CumulativeLength!;
			float lenToSegmentStart = cum[s.SegIdx];
			float segLen = (float)HaversineMeters(a.Lat, a.Lon, b.Lat, b.Lon);
			float lenAlongSegment = (float)(t * segLen);
			float lenFromStart = lenToSegmentStart + lenAlongSegment;
			float lenToEnd = cum[^1] - lenFromStart;

			bestD = d;
			best = new SnapResult(s.FromNode, s.EdgeIdx, s.SegIdx, t, snapLat, snapLon, d, lenFromStart, lenToEnd);
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

// ClipRouter — A* between two snapped points (virtual nodes)
public static class ClipRouter
{
	// Returns the best path between two SnapResult points and its total cost (meters)
	public static (List<int> path, float cost) RouteBetweenSnaps(
	RoadGraph g,
	Snapper.SnapResult A,
	Snapper.SnapResult B)
	{
		// Resolve endpoints of the two edges
		var eA = g.Adj[A.FromNode][A.EdgeIdx];
		int AU = A.FromNode;   // start edge 'from' node
		int AV = eA.To;        // start edge 'to' node

		var eB = g.Adj[B.FromNode][B.EdgeIdx];
		int BU = B.FromNode;   // goal edge 'from' node
		int BV = eB.To;        // goal edge 'to' node

		//  SAME EDGE SPECIAL CASE (same directed edge OR reverse of it)
		bool sameDirected = (A.FromNode == B.FromNode && A.EdgeIdx == B.EdgeIdx);
		bool reversePair = (A.FromNode == eB.To && eA.To == B.FromNode); // B is the reverse of A
		if (sameDirected || reversePair)
		{
			var L = eA.CumulativeLength![^1];           // full length of A's edge (AU->AV orientation)
			float posA = A.LenFromStart;      // position of A along A's edge (AU->AV)
			float posB = sameDirected ? B.LenFromStart : (L - B.LenFromStart); // map B to same orientation

			float delta = Math.Abs(posB - posA);

			// Minimal node path in the right direction along the edge
			List<int> nodePath = (posB >= posA)
				? new List<int> { AU, AV }   // travel AU -> AV
				: new List<int> { AV, AU };  // travel AV -> AU

			return (nodePath, delta);
		}

		float bestCost = float.PositiveInfinity;
		List<int>? bestPath = null;

		// Four combinations: (AU|AV) -> (BU|BV)
		(int X, float wStartX)[] starts = new[] { (AU, A.LenFromStart), (AV, A.LenToEnd) };
		(int Y, float wYGoal)[] goals = new[] { (BU, B.LenToEnd), (BV, B.LenFromStart) };

		foreach (var (X, wSX) in starts)
			foreach (var (Y, wYG) in goals)
			{
				var pathXY = AStarRouter.ShortestPath(g, X, Y);
				if (pathXY.Count == 0) continue;

				float wXY = SumPath(g, pathXY);
				float total = wSX + wXY + wYG;

				if (total < bestCost)
				{
					bestCost = total;
					bestPath = pathXY;
				}
			}

		return (bestPath ?? new List<int>(), bestCost);
	}


	// Sum edge weights along a node path
	private static float SumPath(RoadGraph g, List<int> path)
	{
		float sum = 0;
		for (int i = 0; i + 1 < path.Count; i++)
		{
			int u = path[i], v = path[i + 1];
			var e = g.Adj[u].FirstOrDefault(ed => ed.To == v);
			if (e != null) sum += e.Weight;
		}
		return sum;
	}

	

}
