using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class SnapAdapter
{
	// Convert Snapper.SnapResult (segment-based) -> generic SnapResult (edge-based)
	public static SnapResult ToGeneric(RoadGraph g, Snapper.SnapResultTmp s)
	{
		var e = g.Adj[s.FromNode][s.EdgeIdx];
		return new SnapResult
		{
			U = s.FromNode,
			V = e.To,
			DistFromU = s.LenFromStart,
			EdgeLen = e.CumulativeLength![^1],
			Point = (s.Lat, s.Lon)
		};
	}
}

