using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed class VirtualEndpointsGraph : IReadOnlyGraph
{
	private readonly IReadOnlyGraph _base;
	private readonly int _nBase;
	private readonly SnapResult _A, _B;

	public int NodeCount => _nBase + 2; // +S +T
	private int S => _nBase;
	private int T => _nBase + 1;

	public VirtualEndpointsGraph(IReadOnlyGraph @base, SnapResult a, SnapResult b)
	{
		_base = @base;
		_nBase = @base.NodeCount;
		_A = a; _B = b;
	}

	public (double Lat, double Lon) GetNodeLatLon(int nodeId)
	{
		if (nodeId == S) return _A.Point;
		if (nodeId == T) return _B.Point;
		return _base.GetNodeLatLon(nodeId);
	}

	public IReadOnlyList<LightEdge> GetAdj(int nodeId)
	{
		// From S: we can go to A.U or A.V with the correct partial cost
		if (nodeId == S)
		{
			float costToU = _A.DistFromU;
			float costToV = _A.EdgeLen - _A.DistFromU;
			return new[]
			{
				new LightEdge(_A.U, costToU),
				new LightEdge(_A.V, costToV)
			};
		}

		// For base nodes: base adj plus (if this node is B.U or B.V) an extra edge to T
		if (nodeId < _nBase)
		{
			var baseAdj = _base.GetAdj(nodeId);
			// Copy base edges
			var list = new List<LightEdge>(baseAdj.Count + 1);
			for (int i = 0; i < baseAdj.Count; i++) list.Add(baseAdj[i]);

			// Add the terminal connector if applicable
			if (nodeId == _B.U)
				list.Add(new LightEdge(T, _B.DistFromU));                // nodeId -> T: partial to snap
			else if (nodeId == _B.V)
				list.Add(new LightEdge(T, _B.EdgeLen - _B.DistFromU));   // nodeId -> T: partial to snap

			return list;
		}

		// From T: sink (no outgoing)
		if (nodeId == T) return Array.Empty<LightEdge>();
		return Array.Empty<LightEdge>();
	}
}

