namespace APUS.Server.Routing
{
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
}
