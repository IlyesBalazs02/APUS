using OSMRouting;

namespace APUS.Server.Routing
{
	public class CreateRoute
	{
		public async Task<List<(double latitude, double longitude)>> GetRouteAsync(
			(double latitude, double longitude) start,
			(double latitude, double longitude) finish)
		{
			var pbfPath = @"D:\tmp\tiles1deg";

			double padLat = 0.001;  // ~100m north/south
			double padLon = 0.0015; // ~100-150m east/west

			double minLat = Math.Min(start.latitude, finish.latitude) - padLat;
			double maxLat = Math.Max(start.latitude, finish.latitude) + padLat;
			double minLon = Math.Min(start.longitude, finish.longitude) - padLon;
			double maxLon = Math.Max(start.longitude, finish.longitude) + padLon;

			var http = new OsmHttpRequest(pbfPath, minLat, minLon, maxLat, maxLon);

			var graph = new GraphBuilder(http.GetOsmXml());
			var graphNodes = graph.GraphNodeList();

			var tree = new KDTree(graphNodes);

			var startNode = tree.FindNearest(start.latitude, start.longitude);
			var endNode = tree.FindNearest(finish.latitude, finish.longitude);


			//BFS Check
			bool reachable = false;
			var visited = new HashSet<GraphNode>();
			var q = new Queue<GraphNode>();
			q.Enqueue(startNode);
			visited.Add(startNode);

			while (q.Count > 0)
			{
				var u = q.Dequeue();
				if (u.Equals(endNode))
				{
					reachable = true;
					break;
				}
				foreach (var kv in u.Neighbours)
				{
					var v = kv.Key;
					if (!visited.Contains(v))
					{
						visited.Add(v);
						q.Enqueue(v);
					}
				}
			}

			Console.WriteLine($"BFS connectivity test: {(reachable ? "CONNECTED" : "DISCONNECTED")}");


			// 2) Only run A* if BFS says “connected”
			if (!reachable)
			{
				Console.WriteLine("Network is disconnected—no A* will ever find a path. " +
								  "You probably need to expand your bounding box so that roads connecting start→end are included.");
				return null;
			}

			var aStar = new AStar(graphNodes);
			var path = aStar.FindPath(startNode, endNode);
			if (path == null || path.Count == 0)
				Console.WriteLine("A* returned no path, even though BFS said connected—time to add some debug-logging inside A*.");
			else
			{
				Console.WriteLine("A* found a path with " + path.Count + " nodes:");
				foreach (var n in path)
					Console.WriteLine($"  {n.Lat}, {n.Lon}");
			}



			var routeCoordinates = new List<(double latitude, double longitude)>();

			foreach (var node in path)
			{
				Console.WriteLine($"  {node.Lat}, {node.Lon}");
				routeCoordinates.Add((node.Lat, node.Lon));
			}

			return routeCoordinates;
		}
	
	
	}
}
