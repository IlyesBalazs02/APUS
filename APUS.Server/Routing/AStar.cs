using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSMRouting
{
	public class AStar
	{
		private List<GraphNode> graphNodeList;

        public AStar(List<GraphNode> graphNodes)
        {
            graphNodeList = graphNodes;
        }

        public List<GraphNode> FindPath(GraphNode start, GraphNode goal)
        {
            var openList = new List<GraphNode> { start};
            var closedList = new HashSet<GraphNode>();
            var cameFrom = new Dictionary<GraphNode, GraphNode>();

			start.G = 0;
			start.H = start.CalculateHeuristic(goal);

			while (openList.Count > 0)
			{
				var current = openList.OrderBy(node => node.F).First();

				if (current == goal)
					return ReconstructPath(cameFrom, current);

				openList.Remove(current);
				closedList.Add(current);

				foreach (var neighbour in current.Neighbours)
				{
					var neighborNode = neighbour.Key;
					var tentativeG = current.G + neighbour.Value;

					if (closedList.Contains(neighborNode))
						continue;

					if (!openList.Contains(neighborNode))
						openList.Add(neighborNode);
					else if (tentativeG >= neighborNode.G)
						continue;

					cameFrom[neighborNode] = current;
					neighborNode.G = tentativeG;
					neighborNode.H = neighborNode.CalculateHeuristic(goal);
				}
			}

			return null; // Path not found
		}

		private static List<GraphNode> ReconstructPath(Dictionary<GraphNode, GraphNode> cameFrom, GraphNode current)
		{
			var path = new List<GraphNode> { current };
			while (cameFrom.ContainsKey(current))
			{
				current = cameFrom[current];
				path.Add(current);
			}
			path.Reverse();
			return path;
		}

	}
}
