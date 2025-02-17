using System.Globalization;

internal class Program
{

    public static void Main(string[] args)
    {
        var graph = GenerateGraph();
        graph.PrintGraph();
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
        Console.WriteLine($"Nejkratší cesty z {startVertex}:");
        foreach (var kvp in shortestPaths)
        {
            Console.WriteLine($"Cíl: {kvp.Key}, Vzdálenost: {kvp.Value.Distance}, Cesta: {string.Join(" -> ", kvp.Value.Path)}");
        }
    }
}
