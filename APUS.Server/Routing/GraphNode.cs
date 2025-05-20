using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSMRouting
{
	public class GraphNode
	{
		public Dictionary<GraphNode, double> Neighbours { get; set; }
		public double G { get; set; } // Cost from start to this node
		public double H { get; set; } // Heuristic cost to goal
		public double F => G + H; // Total cost

		public double Lat { get; set; }
		public double Lon { get; set; }
		public long Id { get; set; }

		public double CalculateHeuristic(GraphNode goal)
		{
			return Math.Sqrt(Math.Pow(goal.Lat - this.Lat, 2) + Math.Pow(goal.Lon - this.Lon, 2));
		}

		public GraphNode(long id, double lat, double lon)
		{
			Neighbours = new Dictionary<GraphNode, double>();
			Lat = lat;
			Lon = lon;
			Id = id;
		}
	}
}
