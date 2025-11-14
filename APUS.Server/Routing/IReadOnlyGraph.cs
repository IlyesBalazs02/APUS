using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APUS.Server.Routing
{
	public interface IReadOnlyGraph
	{
		int NodeCount { get; }
		(double Lat, double Lon) GetNodeLatLon(int nodeId);
		IReadOnlyList<LightEdge> GetAdj(int nodeId);
	}

	public readonly struct LightEdge
	{
		public readonly int To;
		public readonly float Weight;
		public LightEdge(int to, float w) { To = to; Weight = w; }
	}
}
