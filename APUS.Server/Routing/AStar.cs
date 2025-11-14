using System.Text.Json;

namespace APUS.Server.Routing
{
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

		public static int NearestNode(IReadOnlyGraph g, double lat, double lon)
		{
			int best = -1;
			double bestD2 = double.MaxValue;

			for (int i = 0; i < g.NodeCount; i++)
			{
				var n = g.GetNodeLatLon(i);
				double dLat = n.Lat - lat;
				double dLon = n.Lon - lon;
				double d2 = dLat * dLat + dLon * dLon; // squared degrees, good enough

				if (d2 < bestD2)
				{
					bestD2 = d2;
					best = i;
				}
			}

			return best;
		}

	}
}
