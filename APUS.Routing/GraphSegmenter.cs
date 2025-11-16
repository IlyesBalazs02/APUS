using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/*
 * Input:road graph
 * Output: many smaller tiles
 * */
public static class GraphSegmenter
{
	public static void WriteSharded(RoadGraph g, string outDir,
		double? desiredCellDeg = null, int targetTileMB = 12)
	{
		Directory.CreateDirectory(outDir);
		string tilesDir = Path.Combine(outDir, "tiles");
		Directory.CreateDirectory(tilesDir);

		// Compute graph bbox
		double minLat = double.PositiveInfinity, minLon = double.PositiveInfinity;
		double maxLat = double.NegativeInfinity, maxLon = double.NegativeInfinity;
		for (int i = 0; i < g.Nodes.Count; i++)
		{
			var n = g.Nodes[i];
			minLat = Math.Min(minLat, n.Lat); maxLat = Math.Max(maxLat, n.Lat);
			minLon = Math.Min(minLon, n.Lon); maxLon = Math.Max(maxLon, n.Lon);
		}

		// Choose grid cell size
		// Start with 0.02 (~2km) and adjust a bit if  too big/small by density.
		double cellDeg = desiredCellDeg ?? 0.02;

		// Assign nodes to cells
		var nodeToTile = new int[g.Nodes.Count];
		var tiles = new Dictionary<(int iy, int ix), List<int>>();
		(int iy, int ix) Key(double lat, double lon) => ((int)Math.Floor(lat / cellDeg), (int)Math.Floor(lon / cellDeg));

		for (int i = 0; i < g.Nodes.Count; i++)
		{
			var k = Key(g.Nodes[i].Lat, g.Nodes[i].Lon);
			if (!tiles.TryGetValue(k, out var list)) tiles[k] = list = new List<int>();
			list.Add(i);
		}

		// Build tile metas, write nodes.bin and node_to_tile.bin first
		var manifest = new DiskGraphManifest
		{
			CellDegrees = cellDeg,
			NodeCount = g.Nodes.Count,
			TilesDir = "tiles"
		};

		using (var headers = File.Create(Path.Combine(outDir, "nodes.bin")))
		using (var bw = new BinaryWriter(headers))
		{
			for (int i = 0; i < g.Nodes.Count; i++)
			{
				var n = g.Nodes[i];
				bw.Write(n.Lat);
				bw.Write(n.Lon);
			}
		}

		// Write node->tile mapping
		var tileList = tiles.Keys.ToList();
		tileList.Sort((a, b) => a.iy != b.iy ? a.iy.CompareTo(b.iy) : a.ix.CompareTo(b.ix));

		var keyToId = new Dictionary<(int iy, int ix), int>();
		for (int t = 0; t < tileList.Count; t++) keyToId[tileList[t]] = t;

		using (var mapStream = File.Create(Path.Combine(outDir, "node_to_tile.bin")))
		{
			var buf = new byte[4];
			for (int t = 0; t < tileList.Count; t++)
			{
				var k = tileList[t];
				var list = tiles[k];
				foreach (var nodeId in list)
				{
					nodeToTile[nodeId] = t;
				}
			}
			// nodeId in index order → tileId
			for (int i = 0; i < g.Nodes.Count; i++)
			{
				BinaryPrimitives.WriteInt32LittleEndian(buf, nodeToTile[i]);
				mapStream.Write(buf, 0, 4);
			}
		}

		// For each tile: collect local nodes, outbound edges, and write a compact binary
		int tileId = 0;
		foreach (var k in tileList)
		{
			var nids = tiles[k];
			// Build a HashSet for quick membership
			var local = new HashSet<int>(nids);
			int localEdges = 0;
			// bbox
			double tMinLat = double.PositiveInfinity, tMinLon = double.PositiveInfinity;
			double tMaxLat = double.NegativeInfinity, tMaxLon = double.NegativeInfinity;
			foreach (var id in nids)
			{
				var n = g.Nodes[id];
				tMinLat = Math.Min(tMinLat, n.Lat); tMaxLat = Math.Max(tMaxLat, n.Lat);
				tMinLon = Math.Min(tMinLon, n.Lon); tMaxLon = Math.Max(tMaxLon, n.Lon);
			}

			// Count edges: include edges from local nodes (even if 'To' outside).
			foreach (var u in nids) localEdges += g.Adj[u].Count;

			string adjPath = Path.Combine(tilesDir, $"tile_{tileId}.adj.bin");
			string geomPath = Path.Combine(tilesDir, $"tile_{tileId}.geom.bin");

			// -------------- ADJACENCY TILE --------------
			// Format:
			// [magic: 8 bytes] "RGSHADJ1"
			// [int32] nodeCount
			// [int32] edgeTotalCount
			// NODES: [int32] globalNodeId (no lat/lon; we already have headers)
			// EDGES: per local node:
			//   [int32] uGlobal
			//   [int32] deg
			//   repeat deg:
			//     [int32] toGlobal
			//     [single] weight

			using (var fsAdj = File.Create(adjPath))
			using (var bwAdj = new BinaryWriter(fsAdj))
			{
				// header
				bwAdj.Write(Encoding.ASCII.GetBytes("RGSHADJ1"));
				bwAdj.Write(nids.Count);
				bwAdj.Write(localEdges);

				// nodes
				foreach (var u in nids)
				{
					bwAdj.Write(u);
				}

				// edges
				foreach (var u in nids)
				{
					var adj = g.Adj[u];
					bwAdj.Write(u);
					bwAdj.Write(adj.Count);

					for (int ei = 0; ei < adj.Count; ei++)
					{
						var e = adj[ei];
						bwAdj.Write(e.To);
						bwAdj.Write(e.Weight);
					}
				}
			}

			// -------------- GEOMETRY TILE --------------
			// Format:
			// [magic: 8 bytes] "RGSHGEO1"
			// [int32] nodeCount
			// [int32] edgeTotalCount
			// NODES: [int32] globalNodeId
			// EDGES: per local node:
			//   [int32] uGlobal
			//   [int32] deg
			//   repeat deg:
			//     [int32] toGlobal
			//     [int32] geomCount
			//       repeat geomCount: [double] lat, [double] lon
			//     [int32] cumLenCount
			//       repeat cumLenCount: [single] value

			using (var fsGeom = File.Create(geomPath))
			using (var bwGeom = new BinaryWriter(fsGeom))
			{
				// header
				bwGeom.Write(Encoding.ASCII.GetBytes("RGSHGEO1"));
				bwGeom.Write(nids.Count);
				bwGeom.Write(localEdges);

				// nodes (for symmetry / simple reading)
				foreach (var u in nids)
				{
					bwGeom.Write(u);
				}

				// edges + geometry
				foreach (var u in nids)
				{
					var adj = g.Adj[u];
					bwGeom.Write(u);
					bwGeom.Write(adj.Count);

					for (int ei = 0; ei < adj.Count; ei++)
					{
						var e = adj[ei];
						bwGeom.Write(e.To);

						// geometry
						bwGeom.Write(e.Geometry.Count);
						for (int gi = 0; gi < e.Geometry.Count; gi++)
						{
							bwGeom.Write(e.Geometry[gi].Lat);
							bwGeom.Write(e.Geometry[gi].Lon);
						}

						// cumlen
						var cum = e.CumulativeLength ?? Array.Empty<float>();
						bwGeom.Write(cum.Length);
						for (int ci = 0; ci < cum.Length; ci++) bwGeom.Write(cum[ci]);
					}
				}
			}

			manifest.Tiles.Add(new TileMeta
			{
				TileId = tileId,
				MinLat = tMinLat,
				MinLon = tMinLon,
				MaxLat = tMaxLat,
				MaxLon = tMaxLon,
				NodeCount = nids.Count,
				EdgeCount = localEdges,
				Path = $"tiles/tile_{tileId}.adj.bin",
				GeomPath = $"tiles/tile_{tileId}.geom.bin"
			});

			tileId++;
		}

		manifest.TileCount = manifest.Tiles.Count;
		manifest.NodesHeaderPath = "nodes.bin";

		File.WriteAllText(Path.Combine(outDir, "graph.manifest.json"),
			JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
	}
}
