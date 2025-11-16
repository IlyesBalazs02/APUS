using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using OsmSharp;
using OsmSharp.Streams;
using OsmSharp.Tags;
using APUS.Routing;


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
	public static double ComputeMultiplier(List<(double Lat, double Lon)> geom, APUS.Routing.IElevationSampler? dem)
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