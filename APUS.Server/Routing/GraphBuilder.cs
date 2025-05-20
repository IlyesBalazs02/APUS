using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace OSMRouting
{
	public class GraphBuilder
	{
		private List<Node> nodeList;
		private List<Way> wayList;
		private List<GraphNode> graphNodeList; // Stores all the GraphNode along with the Properties for the A* algorithm
		public GraphBuilder(string xml)
		{
			nodeList = new List<Node>();
			wayList = new List<Way>();
			graphNodeList = new List<GraphNode>();
			ProcessOsmXml(xml);
			CreateGraph(SelectGraphNodes());
		}

		public List<GraphNode> GraphNodeList()
		{
			return graphNodeList;
		}

		//TODO return path with the correct distance
		public List<Node> ReconstructPath(List<GraphNode> mainNodes)
		{
			List<Node> result = new List<Node>();

			Node a = nodeList.Find(t => t.Id == mainNodes.First().Id);
			result.Add(a);

			foreach (var node in mainNodes.Skip(1))
			{
				Node b = nodeList.Find(t => t.Id == node.Id);

				var path = wayList.Find(t => t.Nodes.Contains(a) && t.Nodes.Contains(b));

				int aIndex = path.Nodes.FindIndex(t => t.Id == a.Id);
				int bIndex = path.Nodes.FindIndex(t => t.Id == b.Id);
				if (aIndex < bIndex)
				{
					while (aIndex < bIndex) 
					{
						++aIndex;
						result.Add(path.Nodes[aIndex]);
					}
				}
				else
				{
					while (aIndex > bIndex)
					{
						--aIndex;
						result.Add(path.Nodes[aIndex]);
					}
				}

				a = b;
			}
			

			return result;
		}


		//Inserts the GraphNodes into the graphNodeList collection and finds the edges with the weigths, then inserts them into the neighbours collection.
		//TODO Use the CalculateDistance method only at the end when the final route is returned to reduce computation. until then use something that requires less computation
		private void CreateGraph(List<Node> SelectGraphNodes)
		{
			double distanceBetweenNodes = 0;
			double prevLat;
			double prevLon;

			// Iterates through all nodes to calculate distances between GraphNodes
			foreach (var way in wayList)
			{
				Node lastNode = way.Nodes[0];
				prevLat = way.Nodes[0].Lat;
				prevLon = way.Nodes[0].Lon;
				foreach (var node in way.Nodes)
				{
					distanceBetweenNodes += CalculateDistance(prevLat, prevLon, node.Lat, node.Lon);
					prevLat = node.Lat;
					prevLon = node.Lon;

					// Checks if the node is a GraphNode
					if (SelectGraphNodes.Contains(node))
					{
						//if it's the first element of the collection, than we can't add a neighbour to it yet, we just put it into the graphnodeList collection if it's still not there
						if (node != way.Nodes[0])
						{

							if (!graphNodeList.Any(t => t.Id == node.Id)) graphNodeList.Add(new GraphNode(node.Id, node.Lat, node.Lon));

							GraphNode tmp = graphNodeList.Find(t => t.Id == node.Id);
							GraphNode lastGraphNode = graphNodeList.Find(t => t.Id == lastNode.Id);

							// 2 different way collection may contain the same 2 graphnode leading into an exception
							if (!(tmp.Neighbours.ContainsKey(lastGraphNode)))
							{
								tmp.Neighbours.Add(lastGraphNode, distanceBetweenNodes);
								lastGraphNode.Neighbours.Add(tmp, distanceBetweenNodes);
							}
							else
							{
								// Update the distance if the new one is shorter
								if (distanceBetweenNodes < tmp.Neighbours[lastGraphNode])
								{
									tmp.Neighbours[lastGraphNode] = distanceBetweenNodes;
									lastGraphNode.Neighbours[tmp] = distanceBetweenNodes;
								}
							}

							distanceBetweenNodes = 0;
							lastNode = node;
						}
						else
						{
							// Add the first node if it's not already in the graphNodeList
							if (!graphNodeList.Any(t => t.Id == lastNode.Id)) graphNodeList.Add(new GraphNode(lastNode.Id, lastNode.Lat, lastNode.Lon));
						}
					}
				}

			}

			//foreach (var node in graphNodeList)
			//{
			//	Console.WriteLine(node.Id);
			//	foreach (var item in node.Neighbours)
			//	{
			//		Console.WriteLine("\t" + item.Key.Id + "   " + item.Value);
			//	}
			//}

		}

		// Returns all nodes that form the graph, which are:
		// - Nodes that appear more than once(indicating intersections / multiple ways from that node)
		// - The first and last nodes of a way (indicative of road ends/dead ends, they represent the nodes in the graph that has only 1 edge)

		//TODO Ignore the Nodes that appears twice as they are just continuous parts of the road
		private List<Node> SelectGraphNodes()
		{
			Dictionary<Node, int> nodeCounts = new Dictionary<Node, int>();

			foreach (var item in wayList)
			{

				//The first and last element of a way collection is must be a GraphNode
				if (!nodeCounts.ContainsKey(item.Nodes.First())) nodeCounts[item.Nodes.First()] = 1;
				if (!nodeCounts.ContainsKey(item.Nodes.Last())) nodeCounts[item.Nodes.Last()] = 1;

				// Count how many times each node appears across all way collections
				foreach (var node in item.Nodes)
				{

					if (nodeCounts.ContainsKey(node))
					{
						nodeCounts[node]++;
					}
					else
					{
						nodeCounts[node] = 1;
					}

				}
			}

			// Filter and return nodes that meet the criteria for being a GraphNode
			return nodeCounts.Where(kvp => kvp.Value >= 2).Select(kvp => kvp.Key).ToList();
		}

		// fill the nodeList and wayList collections based on the OSM data
		private void ProcessJsonResponse(string jsonResponse)
		{

			JObject jsonObject = JsonConvert.DeserializeObject<JObject>(jsonResponse);
			JArray elements = jsonObject["elements"] as JArray;

			// Process nodes
			foreach (JObject element in elements)
			{
				string type = element["type"].ToString();
				if (type == "node")
				{
					long id = element["id"].Value<long>();
					double lat = element["lat"].Value<double>();
					double lon = element["lon"].Value<double>();

					nodeList.Add(new Node(id, lat, lon));
				}
			}

			// Process ways
			foreach (JObject element in elements)
			{
				string type = element["type"].ToString();
				if (type == "way")
				{
					long id = element["id"].Value<long>();
					Way way = new Way(id);

					JArray nodesArray = element["nodes"] as JArray;
					foreach (JValue nodeElement in nodesArray)
					{
						long nodeId = nodeElement.Value<long>();
						Node node = nodeList.Find(n => n.Id == nodeId);
						if (node != null)
						{
							//TODO fix it
							if(!way.Nodes.Contains(node))
							way.Nodes.Add(node);
						}
					}

					wayList.Add(way);
				}
			}

		}

		public void ProcessOsmXml(string filePath)
		{
			var doc = XDocument.Load(@"D:\tmp\asdasd.osm");
			nodeList.Clear();
			wayList.Clear();

			// 1) pull out every <node id="…" lat="…" lon="…"/>
			foreach (var xNode in doc.Descendants("node"))
			{
				var id = (long)xNode.Attribute("id");
				var lat = (double)xNode.Attribute("lat");
				var lon = (double)xNode.Attribute("lon");
				nodeList.Add(new Node(id, lat, lon));
			}

			// 2) pull out every <way id="…"><nd ref="…"/>…</way>
			foreach (var xWay in doc.Descendants("way"))
			{
				var way = new Way((long)xWay.Attribute("id"));
				foreach (var xNd in xWay.Elements("nd"))
				{
					var refId = (long)xNd.Attribute("ref");
					var node = nodeList.FirstOrDefault(n => n.Id == refId);
					if (node != null && !way.Nodes.Contains(node))
						way.Nodes.Add(node);
				}
				wayList.Add(way);
			}
		}



		public const double RADIUS_OF_EARTH = 6371; // Earth's radius in kilometers

		//TODO: Only use this if the route is already found

		private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
		{
			// Convert latitude and longitude from degrees to radians
			double lat1Radians = DegreesToRadians(lat1);
			double lon1Radians = DegreesToRadians(lon1);
			double lat2Radians = DegreesToRadians(lat2);
			double lon2Radians = DegreesToRadians(lon2);

			// Calculate differences between latitudes and longitudes
			double latDiff = lat2Radians - lat1Radians;
			double lonDiff = lon2Radians - lon1Radians;

			// Haversine formula
			double a = Math.Pow(Math.Sin(latDiff / 2), 2) +
					   Math.Cos(lat1Radians) * Math.Cos(lat2Radians) *
					   Math.Pow(Math.Sin(lonDiff / 2), 2);
			double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

			// Calculate distance
			double distanceInKilometers = RADIUS_OF_EARTH * c;
			var distanceInMeters = distanceInKilometers * 1000;
			return distanceInMeters;
		}

		private static double DegreesToRadians(double degrees)
		{
			return degrees * Math.PI / 180.0;
		}
	}

}
