using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Routing
{
	public sealed class TileRegistry
	{
		public sealed class TileMeta
		{
			public TileId Id;
			public MacroKey Macro;
			public int LocalTileId;
			public double MinLat, MaxLat;
			public double MinLon, MaxLon;

			public string MacroDirectory(string root) =>
				Path.Combine(root, $"{Macro.LatInt}_{Macro.LonInt}");

			public string NodesPath(string root) =>
				Path.Combine(MacroDirectory(root), $"tile_{LocalTileId:0000}.nodes.bin");

			public string AdjPath(string root) =>
				Path.Combine(MacroDirectory(root), $"tile_{LocalTileId:0000}.adj.bin");

			public string GeomPath(string root) =>
				Path.Combine(MacroDirectory(root), $"tile_{LocalTileId:0000}.geom.bin");
		}

		private readonly string _root;
		private readonly Dictionary<int, TileMeta> _byId = new();

		public TileRegistry(string rootDir)
		{
			_root = rootDir;
			LoadAllTiles();
		}

		private void LoadAllTiles()
		{
			if (!Directory.Exists(_root))
				return;

			foreach (var macroDir in Directory.EnumerateDirectories(_root))
			{
				var macroName = Path.GetFileName(macroDir);
				var parts = macroName.Split('_', StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length != 2)
					continue;
				if (!int.TryParse(parts[0], out var latInt))
					continue;
				if (!int.TryParse(parts[1], out var lonInt))
					continue;

				var mk = new MacroKey(latInt, lonInt);
				string tilesBin = Path.Combine(macroDir, "tiles.bin");
				if (!File.Exists(tilesBin))
					continue;

				using var fs = File.OpenRead(tilesBin);
				using var br = new BinaryReader(fs);

				int tileCount = br.ReadInt32();
				for (int i = 0; i < tileCount; i++)
				{
					int globalTileId = br.ReadInt32();
					int localTileId = br.ReadInt32();
					double minLat = br.ReadDouble();
					double maxLat = br.ReadDouble();
					double minLon = br.ReadDouble();
					double maxLon = br.ReadDouble();

					var meta = new TileMeta
					{
						Id = new TileId(globalTileId),
						Macro = mk,
						LocalTileId = localTileId,
						MinLat = minLat,
						MaxLat = maxLat,
						MinLon = minLon,
						MaxLon = maxLon
					};

					_byId[globalTileId] = meta;
				}
			}
		}

		public TileMeta Get(TileId id) => _byId[id.Value];

		public IEnumerable<TileMeta> GetMacroTiles(MacroKey macro) =>
			_byId.Values.Where(t => t.Macro.LatInt == macro.LatInt &&
									t.Macro.LonInt == macro.LonInt);

		public IEnumerable<TileMeta> AllTiles => _byId.Values;

		public string RootDirectory => _root;
	}

}
