using APUS.Server.Routing;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace APUS.Server.Routing
{
	public sealed class PagedRoadGraph : IReadOnlyGraph, IDisposable
	{
		private readonly string _root;
		private readonly DiskGraphManifest _m;
		private readonly (double Lat, double Lon)[] _nodeHeaders; // from nodes.bin
		private readonly int[] _nodeToTile;     // from node_to_tile.bin (len = NodeCount)

		private readonly int _maxTilesInMem;
		private readonly ConcurrentDictionary<int, TileData> _cache = new();
		private readonly LinkedList<int> _lru = new();
		private readonly object _lock = new();

		public int NodeCount => _m.NodeCount;



		private sealed class TileData
		{
			public int TileId; // which tile this is

			public HashSet<int> NodeSet = new(); // membership
			public Dictionary<int, List<LightEdge>> Adj = new();

			// Lazily filled geometry
			public Dictionary<(int u, int v), (List<(double Lat, double Lon)> geom, float[] cum)> Geoms = new();

			public bool GeomsLoaded = false;
		}


		public PagedRoadGraph(string rootDir, int maxTilesInMem = 8)
		{
			_root = rootDir;
			_maxTilesInMem = Math.Max(2, maxTilesInMem);

			var manifestPath = Path.Combine(rootDir, "graph.manifest.json");
			_m = JsonSerializer.Deserialize<DiskGraphManifest>(File.ReadAllText(manifestPath))
				 ?? throw new Exception("Bad manifest");

			// Load node headers (lat/lon) into memory for fast heuristic
			using (var fs = File.OpenRead(Path.Combine(rootDir, _m.NodesHeaderPath)))
			using (var br = new BinaryReader(fs))
			{
				_nodeHeaders = new (double, double)[_m.NodeCount];
				for (int i = 0; i < _m.NodeCount; i++)
				{
					double lat = br.ReadDouble();
					double lon = br.ReadDouble();
					_nodeHeaders[i] = (lat, lon);
				}
			}

			// Load node->tile map
			_nodeToTile = new int[_m.NodeCount];
			using (var fs = File.OpenRead(Path.Combine(rootDir, _m.NodeToTilePath)))
			{
				var buf = new byte[4];
				for (int i = 0; i < _m.NodeCount; i++)
				{
					fs.Read(buf, 0, 4);
					_nodeToTile[i] = BinaryPrimitives.ReadInt32LittleEndian(buf);
				}
			}
		}

		public (double Lat, double Lon) GetNodeLatLon(int nodeId) => _nodeHeaders[nodeId];

		public IReadOnlyList<LightEdge> GetAdj(int nodeId)
		{
			int tileId = _nodeToTile[nodeId];
			var tile = EnsureTile(tileId);
			if (!tile.Adj.TryGetValue(nodeId, out var list))
				return Array.Empty<LightEdge>();
			return list;
		}

		private TileData EnsureTile(int tileId)
		{
			if (_cache.TryGetValue(tileId, out var t))
			{
				Touch(tileId); return t;
			}
			lock (_lock)
			{
				if (_cache.TryGetValue(tileId, out t))
				{
					Touch(tileId); return t;
				}
				// Evict if needed
				while (_cache.Count >= _maxTilesInMem)
				{
					int evict = _lru.Last!.Value;
					_lru.RemoveLast();
					_cache.TryRemove(evict, out _);
				}
				// Load
				t = LoadTile(tileId);
				_cache[tileId] = t;
				_lru.AddFirst(tileId);
				return t;
			}
		}

		private void Touch(int tileId)
		{
			lock (_lock)
			{
				var node = _lru.Find(tileId);
				if (node != null) { _lru.Remove(node); _lru.AddFirst(node); }
			}
		}

		private TileData LoadTile(int tileId)
		{
			var meta = _m.Tiles.First(x => x.TileId == tileId);
			var tilePath = Path.Combine(_root, meta.Path);

			var td = new TileData { TileId = tileId };

			using var fs = File.OpenRead(tilePath);
			using var br = new BinaryReader(fs);

			var magic = new string(br.ReadChars(8));
			if (magic != "RGSHADJ1") throw new Exception("Corrupt adjacency tile");

			int nCount = br.ReadInt32();
			int eCount = br.ReadInt32();

			var localNodes = new int[nCount];

			// Nodes (only ids)
			for (int i = 0; i < nCount; i++)
			{
				int u = br.ReadInt32();
				localNodes[i] = u;
				td.NodeSet.Add(u);
				td.Adj[u] = new List<LightEdge>(8);
			}

			// Edges (no geometry)
			for (int ni = 0; ni < nCount; ni++)
			{
				int u = br.ReadInt32();
				int deg = br.ReadInt32();
				var list = td.Adj[u];

				for (int k = 0; k < deg; k++)
				{
					int to = br.ReadInt32();
					float w = br.ReadSingle();
					list.Add(new LightEdge(to, w));
				}
			}

			return td;
		}

		private void EnsureGeomLoaded(int tileId, TileData tile)
		{
			if (tile.GeomsLoaded) return;

			var meta = _m.Tiles.First(x => x.TileId == tileId);
			if (string.IsNullOrEmpty(meta.GeomPath))
			{
				// No geometry file – nothing to load.
				tile.GeomsLoaded = true;
				return;
			}

			var geomPath = Path.Combine(_root, meta.GeomPath);
			using var fs = File.OpenRead(geomPath);
			using var br = new BinaryReader(fs);

			var magic = new string(br.ReadChars(8));
			if (magic != "RGSHGEO1") throw new Exception("Corrupt geometry tile");

			int nCount = br.ReadInt32();
			int eCount = br.ReadInt32();

			var localNodes = new int[nCount];
			for (int i = 0; i < nCount; i++)
			{
				localNodes[i] = br.ReadInt32();
			}

			// Edges + geometry
			for (int ni = 0; ni < nCount; ni++)
			{
				int u = br.ReadInt32();
				int deg = br.ReadInt32();

				for (int k = 0; k < deg; k++)
				{
					int to = br.ReadInt32();

					int gc = br.ReadInt32();
					var geom = new List<(double Lat, double Lon)>(gc);
					for (int gi = 0; gi < gc; gi++)
					{
						double la = br.ReadDouble();
						double lo = br.ReadDouble();
						geom.Add((la, lo));
					}

					int cc = br.ReadInt32();
					var cum = new float[cc];
					for (int ci = 0; ci < cc; ci++)
						cum[ci] = br.ReadSingle();

					tile.Geoms[(u, to)] = (geom, cum);
				}
			}

			tile.GeomsLoaded = true;
		}



		public void Dispose()
		{
			_cache.Clear();
			_lru.Clear();
		}

		public bool TryGetEdgeGeometry(int u, int v,
	out List<(double Lat, double Lon)> geom, out float[] cum)
		{
			// First try the tile where 'u' lives
			int tileIdU = _nodeToTile[u];
			var tileU = EnsureTile(tileIdU);
			EnsureGeomLoaded(tileIdU, tileU);

			if (tileU.Geoms.TryGetValue((u, v), out var t))
			{
				geom = t.geom;
				cum = t.cum;
				return true;
			}

			// If not found, the edge may be stored in the tile of 'v'
			int tileIdV = _nodeToTile[v];
			if (tileIdV != tileIdU)
			{
				var tileV = EnsureTile(tileIdV);
				EnsureGeomLoaded(tileIdV, tileV);

				if (tileV.Geoms.TryGetValue((u, v), out t))
				{
					geom = t.geom;
					cum = t.cum;
					return true;
				}
			}

			geom = new List<(double Lat, double Lon)>();
			cum = Array.Empty<float>();
			return false;
		}

		static List<(double Lat, double Lon)> CutByMeters(
	List<(double Lat, double Lon)> geom, float[] cum, float startM, float endM)
		{
			// want the segment from startM to endM *in that order*.
			// Internally  assume cumulative is increasing, so work on [min,max] then
			// reverse the result if the caller gave startM > endM.
			bool reversed = false;
			if (startM > endM)
			{
				(startM, endM) = (endM, startM);
				reversed = true;
			}

			var outPts = new List<(double Lat, double Lon)>(geom.Count);

			if (geom.Count == 0) return outPts;
			if (cum.Length != geom.Count) throw new InvalidOperationException("cum length mismatch");

			// Find segment containing startM
			int i = Array.BinarySearch(cum, startM);
			if (i < 0) i = ~i;
			i = Math.Max(1, i); // we will look at [i-1, i]

			// interpolate point at startM
			(double latS, double lonS) =
				InterpPoint(geom[i - 1], geom[i], cum[i - 1], cum[i], startM);
			outPts.Add((latS, lonS));

			// add intermediate vertices within (startM, endM)
			for (int k = i; k < cum.Length && cum[k] < endM; k++)
				outPts.Add(geom[k]);

			// interpolate point at endM
			int j = Array.BinarySearch(cum, endM);
			if (j < 0) j = ~j;
			j = Math.Max(1, j);

			(double latE, double lonE) =
				InterpPoint(geom[j - 1], geom[j], cum[j - 1], cum[j], endM);

			// Avoid duplicate last point
			if (outPts.Count == 0 || outPts[^1] != (latE, lonE))
				outPts.Add((latE, lonE));

			// If caller asked for startM > endM, we need to flip direction.
			if (reversed)
				outPts.Reverse();

			return outPts;

			static (double, double) InterpPoint(
				(double Lat, double Lon) a, (double Lat, double Lon) b,
				float ca, float cb, float cx)
			{
				if (cb <= ca) return a;
				double t = (cx - ca) / (cb - ca);
				return (a.Lat + (b.Lat - a.Lat) * t, a.Lon + (b.Lon - a.Lon) * t);
			}
		}


		static void EnsureForward(ref List<(double Lat, double Lon)> geom, ref float[] cum)
		{
			if (geom.Count < 2) return;
			if (cum.Length != geom.Count) return;
			// cumulative must be strictly increasing from start to end; if not, reverse
			if (cum[0] > cum[^1])
			{
				geom.Reverse();
				Array.Reverse(cum);
				// normalize cumulative to start at 0
				float base0 = cum[0];
				for (int i = 0; i < cum.Length; i++) cum[i] -= base0;
			}
		}

		public static List<(double Lat, double Lon)> BuildRoutePolyline(
	PagedRoadGraph g, List<int> path, SnapResult A, SnapResult B)
		{
			var line = new List<(double, double)>(1024);

			if (path.Count < 2)
			{
				// Path is entirely within one edge (S→T), cut directly on A edge
				if (A.U == B.U && A.V == B.V)
				{
					if (!g.TryGetEdgeGeometry(A.U, A.V, out var geom, out var cum))
						return new List<(double, double)>();
					EnsureForward(ref geom, ref cum);

					var part = CutByMeters(geom, cum, A.DistFromU, B.DistFromU);
					line.AddRange(part);
				}
				return line;
			}

			//First hop: S → anchor (anchor is path[1])
			int anchorStart = path[1];
			if (anchorStart == A.U || anchorStart == A.V)
			{
				int u = A.U, v = A.V;
				float fromM = A.DistFromU;
				float toM = (anchorStart == A.U) ? 0f : A.EdgeLen;

				if (!g.TryGetEdgeGeometry(u, v, out var geom, out var cum))
					throw new Exception("Missing geometry for start edge");

				EnsureForward(ref geom, ref cum);
				// If anchoring to U,  go backwards (snap -> U), so cut [fromM .. 0] will be reversed by CutByMeters
				var seg = CutByMeters(geom, cum, fromM, toM);
				line.AddRange(seg);
			}
			else
			{
				// Defensive: if someone anchors to a non-endpoint, skip (shouldn't happen)
				line.Add(A.Point);
			}

			//Internal hops between base nodes
			for (int i = 1; i + 1 < path.Count; i++)
			{
				int u = path[i];
				int v = path[i + 1];
				if (u == v) continue;

				// skip S/T if present
				if (u >= g.NodeCount || v >= g.NodeCount) continue;

				if (!g.TryGetEdgeGeometry(u, v, out var geom, out var cum))
				{
					// Try reverse (some data stores geometry only once)
					if (!g.TryGetEdgeGeometry(v, u, out geom, out cum))
						throw new Exception($"Missing geometry for edge {u}->{v}");
					// reverse for u->v direction
					geom.Reverse();
					Array.Reverse(cum);
					float base0 = cum[0];
					for (int k = 0; k < cum.Length; k++) cum[k] -= base0;
				}
				else
				{
					EnsureForward(ref geom, ref cum);
				}

				// Append, avoiding duplicate vertex at join
				if (line.Count > 0 && line[^1] == geom[0])
					line.AddRange(geom.Skip(1));
				else
					line.AddRange(geom);
			}

			// 3) Last hop: last anchor → T (path[^2] → T)
			int anchorEnd = path[^2];
			if (anchorEnd == B.U || anchorEnd == B.V)
			{
				int u = B.U, v = B.V;
				float fromM = (anchorEnd == B.U) ? 0f : B.EdgeLen;
				float toM = B.DistFromU;

				if (!g.TryGetEdgeGeometry(u, v, out var geom, out var cum))
					throw new Exception("Missing geometry for end edge");

				EnsureForward(ref geom, ref cum);
				var seg = CutByMeters(geom, cum, fromM, toM);

				if (line.Count > 0 && line[^1] == seg[0])
					line.AddRange(seg.Skip(1));
				else
					line.AddRange(seg);
			}
			else
			{
				// Defensive: add end point
				if (line.Count == 0 || line[^1] != B.Point) line.Add(B.Point);
			}

			return line;
		}


		public static List<(double Lat, double Lon)> GetRouteGeometryBetweenCoords(
	PagedRoadGraph storage,
	(double Lat, double Lon) start,
	(double Lat, double Lon) end)
		{
			// Snap both points (plug in your real snapper here)
			SnapResult A = SnapToGraph(storage, start);
			SnapResult B = SnapToGraph(storage, end);

			// Build overlay graph and run A*
			var overlay = new VirtualEndpointsGraph(storage, A, B);
			int S = storage.NodeCount;        // as in overlay
			int T = storage.NodeCount + 1;

			var nodePath = AStarRouter.ShortestPath(overlay, S, T);

			// Build the full polyline from edges (+ partials)
			var poly = BuildRoutePolyline(storage, nodePath, A, B);
			return poly;
		}

		static SnapResult SnapToGraph(PagedRoadGraph g, (double Lat, double Lon) p)
		{
			// TODO: connect segment index; for now, throw:
			throw new NotImplementedException("Integrate your snapper here.");
		}



	}


}
