using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Routing
{
	public readonly struct MacroKey
	{
		public readonly int LatInt;
		public readonly int LonInt;
		public MacroKey(int latInt, int lonInt)
		{
			LatInt = latInt;
			LonInt = lonInt;
		}

		public override string ToString() => $"{LatInt}_{LonInt}";
	}

	public sealed class TileBuilder
	{
		public int GlobalTileId;
		public MacroKey Macro;
		public int LocalTileId;

		public double MinLat, MaxLat;
		public double MinLon, MaxLon;

		public List<int> NodeIndices = new();
		public List<(int FromLocal, LightEdgeOnDisk Edge)> Edges = new();
	}

	public static class GraphSegmenter
	{
		public static void WriteMultiLevel(RoadGraph g, string outDir,
			int targetTileMB = 12, int maxNodesPerTile = 50_000)
		{
			Directory.CreateDirectory(outDir);

			// Group nodes by macro
			var macroToNodeIds = new Dictionary<MacroKey, List<int>>();
			for (int i = 0; i < g.Nodes.Count; i++)
			{
				var n = g.Nodes[i];
				int latInt = (int)Math.Floor(n.Lat);
				int lonInt = (int)Math.Floor(n.Lon);
				var mk = new MacroKey(latInt, lonInt);

				if (!macroToNodeIds.TryGetValue(mk, out var list))
					macroToNodeIds[mk] = list = new List<int>();
				list.Add(i);
			}

			// For each macro, recursively split into subtiles based on density
			var allTiles = new List<TileBuilder>();
			int nextGlobalTileId = 0;

			foreach (var kvp in macroToNodeIds)
			{
				var macro = kvp.Key;
				var nodeIds = kvp.Value;

				// Compute macro bbox
				double minLat = double.PositiveInfinity, maxLat = double.NegativeInfinity;
				double minLon = double.PositiveInfinity, maxLon = double.NegativeInfinity;
				foreach (var idx in nodeIds)
				{
					var n = g.Nodes[idx];
					minLat = Math.Min(minLat, n.Lat);
					maxLat = Math.Max(maxLat, n.Lat);
					minLon = Math.Min(minLon, n.Lon);
					maxLon = Math.Max(maxLon, n.Lon);
				}

				// Recursive split into subtiles
				var macroTiles = SplitMacroIntoTiles(
					g, macro, nodeIds, minLat, maxLat, minLon, maxLon,
					maxNodesPerTile);

				// Assign global tile IDs and local tile IDs
				int localId = 0;
				foreach (var t in macroTiles)
				{
					t.GlobalTileId = nextGlobalTileId++;
					t.LocalTileId = localId++;
					allTiles.Add(t);
				}
			}

			// Build edges per tile
			BuildTileEdges(g, allTiles);

			// Write macros and tile-files
			WriteTilesToDisk(outDir, allTiles, g);
		}

		private static List<TileBuilder> SplitMacroIntoTiles(
	RoadGraph g,
	MacroKey macro,
	List<int> nodeIds,
	double minLat, double maxLat,
	double minLon, double maxLon,
	int maxNodesPerTile,
	int depth = 0)
		{
			if (nodeIds.Count <= maxNodesPerTile || depth > 10)
			{
				var tb = new TileBuilder
				{
					Macro = macro,
					MinLat = minLat,
					MaxLat = maxLat,
					MinLon = minLon,
					MaxLon = maxLon,
					NodeIndices = nodeIds
				};
				return new List<TileBuilder> { tb };
			}

			// Split bbox into 4 quadrants
			double midLat = 0.5 * (minLat + maxLat);
			double midLon = 0.5 * (minLon + maxLon);

			var quads = new[]
			{
			(minLat, midLat, minLon, midLon),
			(minLat, midLat, midLon, maxLon),
			(midLat, maxLat, minLon, midLon),
			(midLat, maxLat, midLon, maxLon),
		};

			var result = new List<TileBuilder>();

			foreach (var (la0, la1, lo0, lo1) in quads)
			{
				var subset = new List<int>();
				foreach (var idx in nodeIds)
				{
					var n = g.Nodes[idx];
					if (n.Lat >= la0 && n.Lat <= la1 &&
						n.Lon >= lo0 && n.Lon <= lo1)
					{
						subset.Add(idx);
					}
				}

				if (subset.Count == 0)
					continue;

				result.AddRange(SplitMacroIntoTiles(
					g, macro, subset, la0, la1, lo0, lo1, maxNodesPerTile, depth + 1));
			}

			return result;
		}

		private sealed class NodeLocation
		{
			public TileBuilder Tile = null!;
			public int LocalIndex;
		}

		private static void BuildTileEdges(RoadGraph g, List<TileBuilder> tiles)
		{
			var locationByNode = new NodeLocation[g.Nodes.Count];

			// Fill NodeLocation
			foreach (var tile in tiles)
			{
				for (int i = 0; i < tile.NodeIndices.Count; i++)
				{
					int nodeIdx = tile.NodeIndices[i];
					locationByNode[nodeIdx] = new NodeLocation
					{
						Tile = tile,
						LocalIndex = i
					};
				}
			}

			// Initialize adjacency lists per tile
			foreach (var tile in tiles)
			{
				tile.Edges = new List<(int, LightEdgeOnDisk)>();
			}

			for (int u = 0; u < g.Nodes.Count; u++)
			{
				var locU = locationByNode[u];
				var fromTile = locU.Tile;
				int fromLocal = locU.LocalIndex;

				foreach (var e in g.Adj[u])
				{
					int v = e.To;
					var locV = locationByNode[v];

					var edge = new LightEdgeOnDisk
					{
						ToTileId = locV.Tile.GlobalTileId,
						ToLocalNode = locV.LocalIndex,
						Cost = e.Weight
					};

					fromTile.Edges.Add((fromLocal, edge));
				}
			}
		}

		private static void WriteTilesToDisk(string outDir, List<TileBuilder> allTiles, RoadGraph g)
		{
			// Group by macro
			var byMacro = allTiles.GroupBy(t => t.Macro);

			foreach (var macroGroup in byMacro)
			{
				var macro = macroGroup.Key;
				string macroDir = Path.Combine(outDir, macro.ToString());
				Directory.CreateDirectory(macroDir);

				var tiles = macroGroup.OrderBy(t => t.LocalTileId).ToList();

				// Write tiles.bin
				string tilesBinPath = Path.Combine(macroDir, "tiles.bin");
				using (var fs = File.Create(tilesBinPath))
				using (var bw = new BinaryWriter(fs))
				{
					bw.Write(tiles.Count);
					foreach (var t in tiles)
					{
						bw.Write(t.GlobalTileId);
						bw.Write(t.LocalTileId);
						bw.Write(t.MinLat);
						bw.Write(t.MaxLat);
						bw.Write(t.MinLon);
						bw.Write(t.MaxLon);
					}
				}

				// Write each tile's nodes and adj
				foreach (var t in tiles)
				{
					string baseName = $"tile_{t.LocalTileId:0000}";
					string nodesPath = Path.Combine(macroDir, baseName + ".nodes.bin");
					string adjPath = Path.Combine(macroDir, baseName + ".adj.bin");

					// Nodes
					using (var fs = File.Create(nodesPath))
					using (var bw = new BinaryWriter(fs))
					{
						bw.Write(t.NodeIndices.Count);
						foreach (var globalIdx in t.NodeIndices)
						{
							var n = g.Nodes[globalIdx];
							bw.Write((float)n.Lat);
							bw.Write((float)n.Lon);
						}
					}

					// CSR adjacency
					int nodeCount = t.NodeIndices.Count;
					var perNodeEdges = new List<LightEdgeOnDisk>[nodeCount];
					for (int i = 0; i < nodeCount; i++) perNodeEdges[i] = new List<LightEdgeOnDisk>();

					foreach (var (fromLocal, edge) in t.Edges)
					{
						perNodeEdges[fromLocal].Add(edge);
					}

					int edgeCount = perNodeEdges.Sum(l => l.Count);
					var edgeIndex = new int[nodeCount + 1];
					var flatEdges = new LightEdgeOnDisk[edgeCount];

					int cursor = 0;
					for (int u = 0; u < nodeCount; u++)
					{
						edgeIndex[u] = cursor;
						var list = perNodeEdges[u];
						foreach (var e in list)
						{
							flatEdges[cursor++] = e;
						}
					}
					edgeIndex[nodeCount] = cursor;

					// Write adjacency
					using (var fs = File.Create(adjPath))
					using (var bw = new BinaryWriter(fs))
					{
						bw.Write(nodeCount);
						bw.Write(edgeCount);
						for (int i = 0; i < edgeIndex.Length; i++)
							bw.Write(edgeIndex[i]);
						for (int i = 0; i < flatEdges.Length; i++)
						{
							var e = flatEdges[i];
							bw.Write(e.ToTileId);
							bw.Write(e.ToLocalNode);
							bw.Write(e.Cost);
						}
					}

					// For now, geom.bin can reuse your existing logic, per tile; omitted here.
				}
			}
		}
	}


}

