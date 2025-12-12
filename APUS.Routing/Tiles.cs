using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Routing
{
	public readonly struct TileId
	{
		public readonly int Value;
		public TileId(int value) => Value = value;
		public override string ToString() => Value.ToString();
	}

	public struct TileInfo
	{
		public int GlobalTileId; 
		public int LocalTileId; 
		public double MinLat, MaxLat;
		public double MinLon, MaxLon;
	}

	public struct NodeRecord
	{
		public float Lat;
		public float Lon;
	}

	public struct LightEdgeOnDisk
	{
		public int ToTileId;     // global tile id
		public int ToLocalNode;  // local index in that tile
		public float Cost;
	}


}
