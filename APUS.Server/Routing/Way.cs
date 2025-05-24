using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace OSMRouting
{
	public class Way
	{
		public Way(long id)
		{
			Id = id;
			Nodes = new List<Node> { };
		}

		public long Id { get; set; }
		public List<Node> Nodes { get; set; }
		public double Distance { get; set; }
	}
}
