using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

	// A single edge in a tile, with both lightweight info for adj.bin ;  geom and cumulative length for geom.bin
	public sealed class TileEdgeRecord
	{
		public int FromLocal;
		public LightEdgeOnDisk Light;
		public IReadOnlyList<(double Lat, double Lon)> Geometry;
		public float[] CumulativeLength;

		public TileEdgeRecord(
			int fromLocal,
			LightEdgeOnDisk light,
			IReadOnlyList<(double Lat, double Lon)> geometry,
			float[] cumulative)
		{
			FromLocal = fromLocal;
			Light = light;
			Geometry = geometry;
			CumulativeLength = cumulative;
		}
	}

	public sealed class TileBuilder
	{
		public int GlobalTileId;
		public MacroKey Macro;
		public int LocalTileId;

		public double MinLat, MaxLat;
		public double MinLon, MaxLon;

		public List<int> NodeIndices = new();

		public List<TileEdgeRecord> Edges = new();
	}

	public static class GraphSegmenter
	{
		public static void WriteMultiLevel(
			RoadGraph g,
			string outDir,
			int maxNodesPerTile = 50_000)
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

				// macro bbox
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

				// Assign global and local tile IDs
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

			// Write macros (folders, tiles.bin) and tile-files (nodes, adj, geom)
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
				(la0: minLat, la1: midLat, lo0: minLon, lo1: midLon),
				(la0: minLat, la1: midLat, lo0: midLon, lo1: maxLon),
				(la0: midLat, la1: maxLat, lo0: minLon, lo1: midLon),
				(la0: midLat, la1: maxLat, lo0: midLon, lo1: maxLon),
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

			// Fill NodeLocation: for each tile, map its local node indices
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

			foreach (var tile in tiles)
			{
				tile.Edges = new List<TileEdgeRecord>();
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

					var light = new LightEdgeOnDisk
					{
						ToTileId = locV.Tile.GlobalTileId,
						ToLocalNode = locV.LocalIndex,
						Cost = e.Weight
					};

					var geom = e.Geometry ?? new List<(double Lat, double Lon)>();
					var cum = e.CumulativeLength ?? Array.Empty<float>();

					fromTile.Edges.Add(new TileEdgeRecord(
						fromLocal,
						light,
						geom,
						cum));
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

				// Write each tile's nodes + adj + geom
				foreach (var t in tiles)
				{
					string baseName = $"tile_{t.LocalTileId:0000}";
					string nodesPath = Path.Combine(macroDir, baseName + ".nodes.bin");
					string adjPath = Path.Combine(macroDir, baseName + ".adj.bin");
					string geomPath = Path.Combine(macroDir, baseName + ".geom.bin");

					//  NODES 
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

					int nodeCount = t.NodeIndices.Count;

					// Group edges
					var perNodeEdges = new List<TileEdgeRecord>[nodeCount];
					for (int i = 0; i < nodeCount; i++) perNodeEdges[i] = new List<TileEdgeRecord>();

					foreach (var edgeRec in t.Edges)
					{
						perNodeEdges[edgeRec.FromLocal].Add(edgeRec);
					}

					// Build CSR for adjacency + a list for geometry
					int edgeCount = perNodeEdges.Sum(l => l.Count);
					var edgeIndex = new int[nodeCount + 1];
					var flatEdges = new LightEdgeOnDisk[edgeCount];
					var flatGeo = new TileEdgeRecord[edgeCount];

					int cursor = 0;
					for (int u = 0; u < nodeCount; u++)
					{
						edgeIndex[u] = cursor;
						var list = perNodeEdges[u];
						foreach (var rec in list)
						{
							flatEdges[cursor] = rec.Light;
							flatGeo[cursor] = rec;
							cursor++;
						}
					}
					edgeIndex[nodeCount] = cursor;

					//  ADJACENCY 
					using (var fs = File.Create(adjPath))
					using (var bw = new BinaryWriter(fs))
					{
						bw.Write(nodeCount);
						bw.Write(edgeCount);

						// edgeIndex
						for (int i = 0; i < edgeIndex.Length; i++)
							bw.Write(edgeIndex[i]);

						// Edges
						for (int i = 0; i < flatEdges.Length; i++)
						{
							var e = flatEdges[i];
							bw.Write(e.ToTileId);
							bw.Write(e.ToLocalNode);
							bw.Write(e.Cost);
						}
					}

					//  GEOMETRY 
					using (var fs = File.Create(geomPath))
					using (var bw = new BinaryWriter(fs))
					{
						bw.Write(nodeCount);
						bw.Write(edgeCount);

						for (int i = 0; i < edgeIndex.Length; i++)
							bw.Write(edgeIndex[i]);

						for (int i = 0; i < flatGeo.Length; i++)
						{
							var rec = flatGeo[i];

							var geom = rec.Geometry ?? Array.Empty<(double Lat, double Lon)>();
							bw.Write(geom.Count);
							foreach (var (lat, lon) in geom)
							{
								bw.Write(lat);
								bw.Write(lon);
							}

							// cumulative lengths
							var cum = rec.CumulativeLength ?? Array.Empty<float>();
							bw.Write(cum.Length);
							foreach (var f in cum)
								bw.Write(f);
						}
					}
				}
			}
		}
	}
}
