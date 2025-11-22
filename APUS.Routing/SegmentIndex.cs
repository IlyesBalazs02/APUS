using System;
using System.Collections.Generic;

namespace APUS.Routing
{

	public sealed class SegmentIndex
	{
		public readonly struct SegRef
		{
			public readonly NodeKey FromNode;
			public readonly NodeKey ToNode;
			public readonly double MinLat, MinLon, MaxLat, MaxLon;

			public SegRef(NodeKey fromNode, NodeKey toNode,
						  double minLat, double minLon,
						  double maxLat, double maxLon)
			{
				FromNode = fromNode;
				ToNode = toNode;
				MinLat = minLat;
				MinLon = minLon;
				MaxLat = maxLat;
				MaxLon = maxLon;
			}
		}

		private readonly Dictionary<(int iy, int ix), List<SegRef>> _grid = new();
		private readonly double _cellDeg;

		public SegmentIndex(double cellDegrees = 0.01)
		{
			_cellDeg = cellDegrees;
		}

		private (int iy, int ix) CellOf(double lat, double lon)
		{
			int iy = (int)Math.Floor(lat / _cellDeg);
			int ix = (int)Math.Floor(lon / _cellDeg);
			return (iy, ix);
		}

		public void AddSegment(NodeKey fromNode, NodeKey toNode,
					   double lat0, double lon0,
					   double lat1, double lon1)
		{
			double minLat = Math.Min(lat0, lat1);
			double maxLat = Math.Max(lat0, lat1);
			double minLon = Math.Min(lon0, lon1);
			double maxLon = Math.Max(lon0, lon1);

			// Compute the range of cells this segment overlaps
			var minCell = CellOf(minLat, minLon);
			var maxCell = CellOf(maxLat, maxLon);

			var segRef = new SegRef(fromNode, toNode, minLat, minLon, maxLat, maxLon);

			for (int iy = minCell.iy; iy <= maxCell.iy; iy++)
			{
				for (int ix = minCell.ix; ix <= maxCell.ix; ix++)
				{
					var cell = (iy, ix);
					if (!_grid.TryGetValue(cell, out var list))
					{
						list = new List<SegRef>();
						_grid[cell] = list;
					}

					list.Add(segRef);
				}
			}
		}



		public IEnumerable<SegRef> Candidates(double lat, double lon, double searchRadiusDeg)
		{
			int r = (int)Math.Ceiling(searchRadiusDeg / _cellDeg);
			var center = CellOf(lat, lon);

			for (int dy = -r; dy <= r; dy++)
			{
				for (int dx = -r; dx <= r; dx++)
				{
					var key = (center.iy + dy, center.ix + dx);
					if (_grid.TryGetValue(key, out var list))
					{
						foreach (var s in list)
							yield return s;
					}
				}
			}
		}

	}
}
