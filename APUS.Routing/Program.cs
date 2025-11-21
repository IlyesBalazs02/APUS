namespace APUS.Routing
{
    internal class Program
    {
        static void Main(string[] args)
        {
			string pbfPath = "hungary-latest.osm.pbf";

			string graphStoreDir = "graph_store";

			if (!File.Exists(pbfPath))
			{
				Console.WriteLine("PBF file not found:");
				Console.WriteLine(pbfPath);
				Console.WriteLine("Please copy your .osm.pbf here or change the path in Program.cs.");
				return;
			}

			// 2) Build & slice graph
			bool needRebuild =
				!Directory.Exists(graphStoreDir) ||
				!Directory.EnumerateFileSystemEntries(graphStoreDir).Any();

			if (needRebuild)
			{
				Console.WriteLine("Building RoadGraph from PBF...");
				RoadGraph g = GraphBuilder.BuildFromPbf(pbfPath, dem: null);

				Console.WriteLine($"Nodes: {g.Nodes.Count}, edges: {g.Adj.Sum(l => l.Count)}");
				Console.WriteLine("Slicing into tiles (graph_store)...");
				GraphSegmenter.WriteMultiLevel(g, graphStoreDir, maxNodesPerTile: 5_000);

				Console.WriteLine("Graph slicing finished.");
			}
			else
			{
				Console.WriteLine("graph_store already exists, skipping rebuild.");
			}
		}
	}
}
