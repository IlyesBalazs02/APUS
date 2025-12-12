using System;
using System.Collections.Generic;

namespace APUS.Routing
{
	// Routes between two snapped points (SnapResult A,B) by creating two virtual
	// nodes S (start) and T (target) on top of the tiled graph and running A*.
	// 
	// States are NodeKey. Normal nodes use real TileIds (>= 0).
	// S and T are represented by special TileIds -1 and -2 and are never stored
	// inside TiledRoadGraph – it handles their neighbors manually.
	public static class VirtualEndpointsGraph
	{
		private static readonly TileId StartTile = new TileId(-1);
		private static readonly TileId EndTile = new TileId(-2);

		private static readonly NodeKey S = new NodeKey(StartTile, 0);
		private static readonly NodeKey T = new NodeKey(EndTile, 0);

		// Route between two snapped positions A and B using the tiled graph.
		// Returns a path of NodeKey from S to T (including both).
		public static List<NodeKey> RouteBetweenSnaps(
			TiledRoadGraph graph,
			SnapResult A,
			SnapResult B)
		{
			return AStarWithVirtual(graph, A, B, S, T);
		}


		private sealed class OpenItem : IComparable<OpenItem>
		{
			public NodeKey Node;
			public float F; // g + h

			public int CompareTo(OpenItem? other)
			{
				if (other is null) return 1;
				return F.CompareTo(other.F);
			}
		}

		private static List<NodeKey> AStarWithVirtual(
			TiledRoadGraph g,
			SnapResult A,
			SnapResult B,
			NodeKey start,
			NodeKey goal)
		{
			var gScore = new Dictionary<NodeKey, float>();
			var cameFrom = new Dictionary<NodeKey, NodeKey?>();

			var open = new PriorityQueue<OpenItem, float>();

			float h0 = Heuristic(g, start, goal, A, B);
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

				foreach (var (neighbor, cost) in GetNeighbors(g, current, A, B))
				{
					if (closed.Contains(neighbor))
						continue;

					float tentativeG = gScore[current] + cost;

					if (!gScore.TryGetValue(neighbor, out var oldG) || tentativeG < oldG)
					{
						gScore[neighbor] = tentativeG;
						cameFrom[neighbor] = current;

						float f = tentativeG + Heuristic(g, neighbor, goal, A, B);
						open.Enqueue(new OpenItem { Node = neighbor, F = f }, f);
					}
				}
			}

			// No path
			return new List<NodeKey>();
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

		private static IEnumerable<(NodeKey Neighbor, float Cost)> GetNeighbors(
			TiledRoadGraph g,
			NodeKey n,
			SnapResult A,
			SnapResult B)
		{
			if (n == S)
			{
				if (!A.U.Equals(A.V))
				{
					yield return (A.U, A.DistFromU);
					yield return (A.V, A.EdgeLen - A.DistFromU);
				}
				else
				{
					yield return (A.U, 0f);
				}
				yield break;
			}

			if (n == T)
				yield break;

			foreach (var (neighbor, cost) in g.GetNeighbors(n))
				yield return (neighbor, cost);

			if (n.Equals(B.U))
			{
				yield return (T, B.DistFromU);
			}
			if (!B.U.Equals(B.V) && n.Equals(B.V))
			{
				yield return (T, B.EdgeLen - B.DistFromU);
			}
		}
		private static float Heuristic(
			TiledRoadGraph g,
			NodeKey u,
			NodeKey v,
			SnapResult A,
			SnapResult B)
		{
			var (lat1, lon1) = GetLatLonForState(g, u, A, B);
			var (lat2, lon2) = GetLatLonForState(g, v, A, B);

			double dLat = lat1 - lat2;
			double dLon = lon1 - lon2;
			return (float)(Math.Sqrt(dLat * dLat + dLon * dLon) * 111_000.0);
		}

		private static (double Lat, double Lon) GetLatLonForState(
			TiledRoadGraph g,
			NodeKey n,
			SnapResult A,
			SnapResult B)
		{
			if (n == S) return A.Point;
			if (n == T) return B.Point;
			return g.GetNodeLatLon(n);
		}
	}
}
