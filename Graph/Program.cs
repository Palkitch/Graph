using System.Globalization;

internal class Program
{
    private static void Main(string[] args)
    {
        // Vytvoření instance grafu
        var graph = new GenericGraph<string, string, int>();
        var startVertex = "A";

        graph.AddVertex("A", "Město A");
        graph.AddVertex("B", "Město B");
        graph.AddVertex("C", "Město C");
        graph.AddVertex("D", "Město D");
        graph.AddVertex("E", "Město E");
        graph.AddVertex("F", "Město F");

        graph.AddEdge("A", "B", 4);
        graph.AddEdge("A", "C", 2);
        graph.AddEdge("B", "D", 3);
        graph.AddEdge("C", "D", 1);
        graph.AddEdge("C", "E", 5);
        graph.AddEdge("D", "E", 4);
        graph.AddEdge("E", "F", 4);


        //var result = graph.FindShortestPaths(startVertex);
        //Console.WriteLine("Původní graf");
        //graph.PrintGraph();

        //Console.WriteLine($"Sousedi nejkratších cest od bodu {startVertex}");
        //graph.PrintPredecessors();


        //var graph = GenerateGraph();
        graph.PrintGraph();
        var shortestPaths = graph.FindShortestPathsFromVertex(startVertex);
        ProcessShortestPaths(startVertex, shortestPaths);

        graph.PrintPredecessors();

        graph.BlockEdge("E", "F");
        Console.WriteLine("Blokace E - F \n");

        graph.PrintGraph();
        shortestPaths = graph.FindShortestPathsFromVertex(startVertex);
        ProcessShortestPaths(startVertex, shortestPaths);

        graph.PrintPredecessors();
    }

    public static GenericGraph<string, string, int> GenerateGraph()
    {
        var graph = new GenericGraph<string, string, int>();
        var random = new Random();
        var vertices = new List<string>();

        // Generování 50 vrcholů
        for (int i = 0; i < 50; i++)
        {
            string key = ((char)('A' + (i / 26))).ToString() + ((char)('A' + (i % 26))).ToString();
            vertices.Add(key);
            graph.AddVertex(key, key);
        }

        // Přidání 100 hran mezi náhodnými vrcholy
        int edgeCount = 0;
        while (edgeCount < 100)
        {
            string from = vertices[random.Next(vertices.Count)];
            string to = vertices[random.Next(vertices.Count)];

            if (from != to && graph.AddEdge(from, to, random.Next(1, 10))) // Náhodná váha mezi 1 a 10
            {
                edgeCount++;
            }
        }
        return graph;
    }
    static void ProcessShortestPaths(string startVertex, Dictionary<string, (int Distance, List<string> Path)> shortestPaths)
    {
        Console.WriteLine($"Nejkratší cesty do {startVertex}:");
        foreach (var kvp in shortestPaths)
        {
            Console.WriteLine($"Cíl: {kvp.Key}, Vzdálenost: {kvp.Value.Distance}, Cesta: {string.Join(" -> ", kvp.Value.Path)}");
        }
    }
}
