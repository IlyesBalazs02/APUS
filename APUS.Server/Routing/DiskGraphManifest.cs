using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Routing
{
	public sealed class DiskGraphManifest
	{
		public string Version { get; set; } = "rg/1";
		public double CellDegrees { get; set; }
		public int NodeCount { get; set; }
		public int TileCount { get; set; }
		public string NodesHeaderPath { get; set; } = "nodes.bin";
		public string TilesDir { get; set; } = "tiles";

		// nodeId -> tileId. Stored separately in node_to_tile.bin
		public string NodeToTilePath { get; set; } = "node_to_tile.bin";

		public List<TileMeta> Tiles { get; set; } = new();
	}

	public sealed class TileMeta
	{
		public int TileId { get; set; }
		public double MinLat { get; set; }
		public double MinLon { get; set; }
		public double MaxLat { get; set; }
		public double MaxLon { get; set; }
		public int NodeCount { get; set; }
		public int EdgeCount { get; set; }
		// Adjacency (light) tile path
		public string Path { get; set; } = "";
		// geometry tile path
		public string GeomPath { get; set; } = "";

	}

}
