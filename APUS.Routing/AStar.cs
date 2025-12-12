using APUS.Routing;

public static class AStarRouter
{
	private sealed class OpenItem : IComparable<OpenItem>
	{
		public NodeKey Node;
		public float F;

		public int CompareTo(OpenItem? other)
		{
			if (other is null) return 1;
			return F.CompareTo(other.F);
		}
	}

	public static List<NodeKey> ShortestPath(TiledRoadGraph g, NodeKey start, NodeKey goal)
	{
		var gScore = new Dictionary<NodeKey, float>();
		var cameFrom = new Dictionary<NodeKey, NodeKey?>();

		var open = new PriorityQueue<OpenItem, float>();

		float h0 = Heuristic(g, start, goal);
		gScore[start] = 0f;
		cameFrom[start] = null;
		open.Enqueue(new OpenItem { Node = start, F = h0 }, h0);

		var closed = new HashSet<NodeKey>();

		while (open.TryDequeue(out var currentItem, out _))
		{
			var current = currentItem.Node;

			if (current.Equals(goal))
				return ReconstructPath(cameFrom, current);

			if (!closed.Add(current))
				continue;

			foreach (var (neighbor, cost) in g.GetNeighbors(current))
			{
				if (closed.Contains(neighbor))
					continue;

				float tentativeG = gScore[current] + cost;

				if (!gScore.TryGetValue(neighbor, out var oldG) || tentativeG < oldG)
				{
					gScore[neighbor] = tentativeG;
					cameFrom[neighbor] = current;

					float f = tentativeG + Heuristic(g, neighbor, goal);
					open.Enqueue(new OpenItem { Node = neighbor, F = f }, f);
				}
			}
		}

		return new List<NodeKey>(); // no path
	}

	private static List<NodeKey> ReconstructPath(
		Dictionary<NodeKey, NodeKey?> cameFrom,
		NodeKey cur)
	{
		var path = new List<NodeKey> { cur };
		while (cameFrom.TryGetValue(cur, out var prev) && prev != null)
		{
			cur = prev.Value;
			path.Add(cur);
		}
		path.Reverse();
		return path;
	}

	private static float Heuristic(TiledRoadGraph g, NodeKey u, NodeKey v)
	{
		var (lat1, lon1) = g.GetNodeLatLon(u);
		var (lat2, lon2) = g.GetNodeLatLon(v);

		double dLat = lat1 - lat2;
		double dLon = lon1 - lon2;
		return (float)(Math.Sqrt(dLat * dLat + dLon * dLon) * 111_000.0);
	}
}
