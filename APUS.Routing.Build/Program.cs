namespace OSMGraphCreater
{
	internal class Program
	{
		static void Main()
		{
			// 1) Build full graph (RAM) and slice to disk
			var g = GraphBuilder.BuildFromPbf("hungary-latest.osm.pbf", dem: null);
			GraphSegmenter.WriteSharded(g, outDir: "graph_store", desiredCellDeg: null, targetTileMB: 12);

			Console.WriteLine("routing:");

			// 2) Open paged (lazy) graph for routing + geometry stitching
			using var pg = new PagedRoadGraph("graph_store", maxTilesInMem: 8);

			// 3) Build a light snap index on the RAM graph (only for snapping)
			var segIndex = new SegmentIndex(g, cellDegrees: 0.01);
			var snapper = new Snapper(g, segIndex);

			// 4) Example coords (lat, lon) – later these will come from the client 47.506827, 19.044823
			var start = (Lat: 47.319523, Lon: 18.088116);
			var end = (Lat: 47.509076, Lon: 17.522093);

			// 5) Snap both points on the RAM graph, then convert to the generic SnapResult
			var snapA_raw = snapper.Snap(start.Lat, start.Lon);
			var snapB_raw = snapper.Snap(end.Lat, end.Lon);

			var A = SnapAdapter.ToGeneric(g, snapA_raw);
			var B = SnapAdapter.ToGeneric(g, snapB_raw);

			// 6) Route over the paged graph using virtual endpoints
			var overlay = new VirtualEndpointsGraph(pg, A, B);
			int S = pg.NodeCount;
			int T = pg.NodeCount + 1;

			var nodePath = AStarRouter.ShortestPath(overlay, S, T);

			// 7) Stitch full coordinate polyline (partial start/end segments + interior edges)
			var line = PagedRoadGraph.BuildRoutePolyline(pg, nodePath, A, B);

			WriteGeoJson(line, "route_line.geojson");

			// 8) (Optional) write GeoJSON
			// WriteGeoJson(line, "route.geojson");
		}

		static void WriteGeoJson(List<(double Lat, double Lon)> line, string path)
		{
			using var sw = new StreamWriter(path);
			sw.Write("{\"type\":\"FeatureCollection\",\"features\":[{\"type\":\"Feature\",\"geometry\":{\"type\":\"LineString\",\"coordinates\":[");
			for (int i = 0; i < line.Count; i++)
			{
				var p = line[i];
				sw.Write($"[{p.Lon.ToString(System.Globalization.CultureInfo.InvariantCulture)},{p.Lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}]");
				if (i + 1 < line.Count) sw.Write(",");
			}
			sw.Write("]},\"properties\":{}}]}");
		}
	}
}
