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
		}

		private readonly Dictionary<int, TileData> _cache = new();
		private readonly LinkedList<int> _lru = new();

		public TiledRoadGraph(string rootDir, int maxTilesInMem = 16)
		{
			_registry = new TileRegistry(rootDir);
			_maxTilesInMem = maxTilesInMem;
		}

		private TileData GetTile(TileId id)
		{
			if (_cache.TryGetValue(id.Value, out var td))
			{
				Touch(id.Value);
				return td;
			}

			// Evict if necessary
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

		// Get number of nodes in a given tile
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
