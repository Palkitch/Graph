using Graph;
using System.Globalization;

internal class Program
{
    public struct Coordinate
    {
        public int X { get; }
        public int Y { get; }
        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }
        public override string ToString() => $"({X}, {Y})";

        public override bool Equals(object? obj) =>
            obj is Coordinate other && X == other.X && Y == other.Y;

        public override int GetHashCode() => HashCode.Combine(X, Y);

    }

    public static void Main(string[] args)
    {

        var graph = GenerateGraph();
        graph.PrintGraph();
        var path = graph.FindShortestPathsFromVertex("BB");
        ProcessShortestPaths("BB", path);
        graph.PrintPredecessors();
    }

    public static GraphWithDijkstra<string, string, int> GenerateGraph()
    {
        var graph = new GraphWithDijkstra<string, string, int>();
        var random = new Random(1);
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
            Console.WriteLine($"Cíl: {kvp.Key}, Vzdálenost: {kvp.Value.Distance}, Cesta: {string.Join(" <- ", kvp.Value.Path)}");
        }
    }
    static void CreateCoordinateGraph()
    {
        var graph = new GraphWithDijkstra<Coordinate, string, int>();
        Coordinate c1 = new Coordinate(0, 0);
        Coordinate c2 = new Coordinate(1, 0);
        Coordinate c3 = new Coordinate(1, 2);
        graph.AddVertex(c1, "a");
        graph.AddVertex(c2, "b");
        graph.AddVertex(c3, "c");

        graph.AddEdge(c1, c2, 3);
        graph.AddEdge(c1, c3, 5);


        graph.PrintGraph();
        graph.PrintPredecessors();
    }
}
