using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Routing
{
	public readonly struct NodeKey : IEquatable<NodeKey>
	{
		public TileId Tile { get; }
		public int LocalIndex { get; }

		public NodeKey(TileId tile, int localIndex)
		{
			Tile = tile;
			LocalIndex = localIndex;
		}

		public bool Equals(NodeKey other) =>
			Tile.Value == other.Tile.Value && LocalIndex == other.LocalIndex;

		public override bool Equals(object? obj) =>
			obj is NodeKey other && Equals(other);

		public override int GetHashCode() =>
			HashCode.Combine(Tile.Value, LocalIndex);

		public static bool operator ==(NodeKey left, NodeKey right) => left.Equals(right);
		public static bool operator !=(NodeKey left, NodeKey right) => !left.Equals(right);

		public override string ToString() => $"[{Tile.Value}:{LocalIndex}]";
	}
}
