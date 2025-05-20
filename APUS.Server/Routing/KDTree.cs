using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSMRouting
{
	public class KDTree
	{
		private class KdTreeNode
		{
			public GraphNode Point;
			public KdTreeNode Left, Right;
			public int Depth;      
			public KdTreeNode(GraphNode p, int depth)
			{
				Point = p;
				Depth = depth;
			}
		}

		private KdTreeNode _root;

		public KDTree(List<GraphNode> points)
		{
			_root = Build(points, 0);
		}

		private KdTreeNode Build(List<GraphNode> pts, int depth)
		{
			if (pts == null || pts.Count == 0)
				return null;

			int axis = depth % 2; 
								  
			pts.Sort((a, b) =>
				axis == 0
					? a.Lat.CompareTo(b.Lat)
					: a.Lon.CompareTo(b.Lon)
			);
			int mid = pts.Count / 2;

			var node = new KdTreeNode(pts[mid], depth);
			// build subtrees without the median
			node.Left = Build(pts.GetRange(0, mid), depth + 1);
			node.Right = Build(pts.GetRange(mid + 1, pts.Count - mid - 1), depth + 1);
			return node;
		}

	
		public GraphNode FindNearest(double queryLat, double queryLon)
		{
			GraphNode best = null;
			double bestDistSq = double.MaxValue;
			Search(_root, queryLat, queryLon, ref best, ref bestDistSq);
			return best;
		}

		private void Search(KdTreeNode node,
							double qLat, double qLon,
							ref GraphNode best,
							ref double bestDistSq)
		{
			if (node == null) return;

			// Compute squared distance from query to this point
			double dLat = node.Point.Lat - qLat;
			double dLon = node.Point.Lon - qLon;
			double distSq = dLat * dLat + dLon * dLon;

			if (distSq < bestDistSq)
			{
				bestDistSq = distSq;
				best = node.Point;
			}

			// Decide which side to visit first
			int axis = node.Depth % 2;
			double diff = (axis == 0) ? (qLat - node.Point.Lat)
									  : (qLon - node.Point.Lon);

			var first = diff < 0 ? node.Left : node.Right;
			var second = diff < 0 ? node.Right : node.Left;

			// Search nearer side
			Search(first, qLat, qLon, ref best, ref bestDistSq);

			if (diff * diff < bestDistSq)
				Search(second, qLat, qLon, ref best, ref bestDistSq);
		}
	}
}
