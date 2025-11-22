using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace APUS.Routing
{
	// Runtime view of the tiled road graph. Loads tiles on demand and keeps an LRU cache.
	public sealed class TiledRoadGraph : IDisposable
	{
		private readonly TileRegistry _registry;
		private readonly int _maxTilesInMem;

		private sealed class TileData
		{
			public NodeRecord[] Nodes = Array.Empty<NodeRecord>();
			public int[] EdgeIndex = Array.Empty<int>();
			public LightEdgeOnDisk[] Edges = Array.Empty<LightEdgeOnDisk>();

			public (double Lat, double Lon)[][] EdgeGeometry = Array.Empty<(double, double)[]>();
			public float[][] EdgeCumulative = Array.Empty<float[]>();
		}

		private readonly Dictionary<int, TileData> _cache = new();
		private readonly LinkedList<int> _lru = new();
		private readonly object _cacheLock = new();

		public TiledRoadGraph(string rootDir, int maxTilesInMem = 16)
		{
			_registry = new TileRegistry(rootDir);
			_maxTilesInMem = maxTilesInMem;

			_lru = new LinkedList<int>();
			_cache = new Dictionary<int, TileData>();
		}

		private TileData GetTile(TileId id)
		{
			lock (_cacheLock)
			{
				if (_cache.TryGetValue(id.Value, out var td))
				{
					Touch(id.Value);
					return td;
				}

				while (_cache.Count >= _maxTilesInMem && _lru.Last != null)
				{
					int evict = _lru.Last.Value;
					_lru.RemoveLast();
					_cache.Remove(evict);
				}

				td = LoadTile(id);
				_cache[id.Value] = td;
				_lru.AddFirst(id.Value);
				return td;
			}
		}


		private void Touch(int tileId)
		{
			var node = _lru.Find(tileId);
			if (node != null)
			{
				_lru.Remove(node);
				_lru.AddFirst(node);
			}
		}

		private TileData LoadTile(TileId id)
		{
			var meta = _registry.Get(id);
			var root = _registry.RootDirectory;

			var td = new TileData();

			// Nodes
			using (var fs = File.OpenRead(meta.NodesPath(root)))
			using (var br = new BinaryReader(fs))
			{
				int nodeCount = br.ReadInt32();
				td.Nodes = new NodeRecord[nodeCount];
				for (int i = 0; i < nodeCount; i++)
				{
					td.Nodes[i] = new NodeRecord
					{
						Lat = br.ReadSingle(),
						Lon = br.ReadSingle()
					};
				}
			}

			// Adjacency
			using (var fs = File.OpenRead(meta.AdjPath(root)))
			using (var br = new BinaryReader(fs))
			{
				int nodeCount = br.ReadInt32();
				int edgeCount = br.ReadInt32();

				td.EdgeIndex = new int[nodeCount + 1];
				for (int i = 0; i < td.EdgeIndex.Length; i++)
					td.EdgeIndex[i] = br.ReadInt32();

				td.Edges = new LightEdgeOnDisk[edgeCount];
				for (int i = 0; i < edgeCount; i++)
				{
					td.Edges[i] = new LightEdgeOnDisk
					{
						ToTileId = br.ReadInt32(),
						ToLocalNode = br.ReadInt32(),
						Cost = br.ReadSingle()
					};
				}
			}

			// Geometry 
			var geomPath = meta.GeomPath(root);
			if (File.Exists(geomPath))
			{
				using (var fs = File.OpenRead(geomPath))
				using (var br = new BinaryReader(fs))
				{
					int nodeCountG = br.ReadInt32();
					int edgeCountG = br.ReadInt32();

					// edgeIndex from geom file
					var geomIndex = new int[nodeCountG + 1];
					for (int i = 0; i < geomIndex.Length; i++)
						geomIndex[i] = br.ReadInt32();

					td.EdgeGeometry = new (double, double)[edgeCountG][];
					td.EdgeCumulative = new float[edgeCountG][];

					for (int i = 0; i < edgeCountG; i++)
					{
						int geomCount = br.ReadInt32();
						var geom = new (double, double)[geomCount];
						for (int j = 0; j < geomCount; j++)
						{
							double lat = br.ReadDouble();
							double lon = br.ReadDouble();
							geom[j] = (lat, lon);
						}
						td.EdgeGeometry[i] = geom;

						int cumCount = br.ReadInt32();
						var cum = new float[cumCount];
						for (int j = 0; j < cumCount; j++)
							cum[j] = br.ReadSingle();
						td.EdgeCumulative[i] = cum;
					}
				}
			}
			else
			{
				int edgeCount = td.Edges.Length;
				td.EdgeGeometry = new (double, double)[edgeCount][];
				td.EdgeCumulative = new float[edgeCount][];
			}

			return td;
		}



		public (double Lat, double Lon) GetNodeLatLon(NodeKey n)
		{
			var tile = GetTile(n.Tile);
			var node = tile.Nodes[n.LocalIndex];
			return (node.Lat, node.Lon);
		}

		// Enumerate neighbors of a node as (NodeKey neighbor, cost).
		public IEnumerable<(NodeKey Neighbor, float Cost)> GetNeighbors(NodeKey n)
		{
			var tile = GetTile(n.Tile);
			int start = tile.EdgeIndex[n.LocalIndex];
			int end = tile.EdgeIndex[n.LocalIndex + 1];

			for (int i = start; i < end; i++)
			{
				var e = tile.Edges[i];
				var nk = new NodeKey(new TileId(e.ToTileId), e.ToLocalNode);
				yield return (nk, e.Cost);
			}
		}

		// Returns the stored geometry for the edge from 'from' to 'to', or null if not found.
		public IReadOnlyList<(double Lat, double Lon)>? GetEdgeGeometry(NodeKey from, NodeKey to)
		{
			var tile = GetTile(from.Tile);
			int start = tile.EdgeIndex[from.LocalIndex];
			int end = tile.EdgeIndex[from.LocalIndex + 1];

			for (int i = start; i < end; i++)
			{
				var e = tile.Edges[i];
				if (e.ToTileId == to.Tile.Value && e.ToLocalNode == to.LocalIndex)
				{
					if (tile.EdgeGeometry != null &&
						i >= 0 && i < tile.EdgeGeometry.Length)
					{
						return tile.EdgeGeometry[i];
					}
					break;
				}
			}

			return null;
		}

		/// Geometry along a directed edge from distance distA to distB (meters),
		public IReadOnlyList<(double Lat, double Lon)> GetPartialEdgeGeometry(
			NodeKey from,
			NodeKey to,
			float distA,
			float distB)
		{
			var tile = GetTile(from.Tile);
			int start = tile.EdgeIndex[from.LocalIndex];
			int end = tile.EdgeIndex[from.LocalIndex + 1];

			int edgeIdx = -1;
			for (int i = start; i < end; i++)
			{
				var e = tile.Edges[i];
				if (e.ToTileId == to.Tile.Value && e.ToLocalNode == to.LocalIndex)
				{
					edgeIdx = i;
					break;
				}
			}

			// Fallback: straight line if it can't find geometry
			if (edgeIdx == -1 ||
				tile.EdgeGeometry == null ||
				edgeIdx >= tile.EdgeGeometry.Length ||
				tile.EdgeCumulative == null ||
				edgeIdx >= tile.EdgeCumulative.Length)
			{
				var (latFrom, lonFrom) = GetNodeLatLon(from);
				var (latTo, lonTo) = GetNodeLatLon(to);
				return new (double Lat, double Lon)[] { (latFrom, lonFrom), (latTo, lonTo) };
			}

			var geom = tile.EdgeGeometry[edgeIdx];
			var cum = tile.EdgeCumulative[edgeIdx];

			if (geom == null || geom.Length == 0 || cum == null || cum.Length != geom.Length)
			{
				var (latFrom, lonFrom) = GetNodeLatLon(from);
				var (latTo, lonTo) = GetNodeLatLon(to);
				return new (double Lat, double Lon)[] { (latFrom, lonFrom), (latTo, lonTo) };
			}

			float totalLen = cum[cum.Length - 1];
			if (totalLen <= 0)
				return geom;

			float clampA = Math.Max(0, Math.Min(totalLen, distA));
			float clampB = Math.Max(0, Math.Min(totalLen, distB));

			bool reverse = clampA > clampB;
			float startD = Math.Min(clampA, clampB);
			float endD = Math.Max(clampA, clampB);

			var result = new List<(double Lat, double Lon)>();

			(double Lat, double Lon) SampleAt(float d)
			{
				if (d <= 0 || cum.Length == 1)
					return geom[0];
				if (d >= totalLen)
					return geom[cum.Length - 1];

				int idx = Array.BinarySearch(cum, d);
				if (idx >= 0)
					return geom[idx];

				idx = ~idx;
				int i0 = idx - 1;
				int i1 = idx;
				float segLen = cum[i1] - cum[i0];
				if (segLen <= 0)
					return geom[i0];

				float tSeg = (d - cum[i0]) / segLen;
				double lat = geom[i0].Lat + (geom[i1].Lat - geom[i0].Lat) * tSeg;
				double lon = geom[i0].Lon + (geom[i1].Lon - geom[i0].Lon) * tSeg;
				return (lat, lon);
			}

			// start point
			var pStart = SampleAt(startD);
			result.Add(pStart);

			// inner vertices strictly between startD and endD
			for (int i = 0; i < cum.Length; i++)
			{
				float d = cum[i];
				if (d > startD && d < endD)
					result.Add(geom[i]);
			}

			// end point
			var pEnd = SampleAt(endD);
			if (pEnd.Lat != result[result.Count - 1].Lat || pEnd.Lon != result[result.Count - 1].Lon)
				result.Add(pEnd);

			if (reverse)
				result.Reverse();

			return result;
		}

		// Get number of nodes in a given tile.
		public int GetNodeCount(TileId tileId)
		{
			var tile = GetTile(tileId);
			return tile.Nodes.Length;
		}

		// Enumerate all tile ids known to this graph
		public IEnumerable<TileId> GetAllTileIds()
		{
			return _registry.AllTiles.Select(t => t.Id);
		}

		// Try to get the cost of the directed edge from 'from' to 'to'.
		public bool TryGetEdgeCost(NodeKey from, NodeKey to, out float cost)
		{
			var tile = GetTile(from.Tile);
			int start = tile.EdgeIndex[from.LocalIndex];
			int end = tile.EdgeIndex[from.LocalIndex + 1];

			for (int i = start; i < end; i++)
			{
				var e = tile.Edges[i];
				if (e.ToTileId == to.Tile.Value && e.ToLocalNode == to.LocalIndex)
				{
					cost = e.Cost;
					return true;
				}
			}

			cost = 0;
			return false;
		}

		public void Dispose()
		{
			_cache.Clear();
			_lru.Clear();
		}
	}
}
