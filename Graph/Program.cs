using Graph;
using System.Globalization;

public class Program
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
        //var graph = GrafZadani();
        //Console.WriteLine(graph.PrintGraph());
        //graph.FindShortestPathsFromVertex('x');
        //Console.WriteLine(graph.PrintShortestPathsTable('x'));

    }



    public static DijkstraGraph<string, string, int> GenerateGraph()
    {
        var graph = new DijkstraGraph<string, string, int>();
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
    static void ProcessShortestPaths(string startVertex, Dictionary<char, (int Distance, List<char> Path)> shortestPaths)
    {
        Console.WriteLine($"Nejkratší cesty z {startVertex}:");
        foreach (var kvp in shortestPaths)
        {
            Console.WriteLine($"Cíl: {kvp.Key}, Vzdálenost: {kvp.Value.Distance}, Cesta: {string.Join(" <- ", kvp.Value.Path)}");
        }
    }
    static void CreateCoordinateGraph()
    {
        var graph = new DijkstraGraph<Coordinate, string, int>();
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
    public DijkstraGraph<string, string, int> GrafZadani()
    {
        var graph = new DijkstraGraph<string, string, int>();
        graph.AddVertex("x", "Mesto X");
        graph.AddVertex("i", "Mesto I");
        graph.AddVertex("a", "Mesto A");
        graph.AddVertex("m", "Mesto M");
        graph.AddVertex("g", "Mesto G");
        graph.AddVertex("u", "Mesto U");
        graph.AddVertex("s", "Mesto S");
        graph.AddVertex("k", "Mesto K");
        graph.AddVertex("t", "Mesto T");
        graph.AddVertex("n", "Mesto N");
        graph.AddVertex("z", "Mesto Z");
        graph.AddVertex("p", "Mesto P");
        graph.AddVertex("r", "Mesto R");
        graph.AddVertex("f", "Mesto F");
        graph.AddVertex("w", "Mesto W");

        graph.AddEdge("s", "i", 3);
        graph.AddEdge("s", "a", 8);
        graph.AddEdge("x", "i", 6);
        graph.AddEdge("x", "a", 3);
        graph.AddEdge("x", "m", 8);
        graph.AddEdge("x", "g", 8);
        graph.AddEdge("x", "u", 10);
        graph.AddEdge("g", "m", 1);
        graph.AddEdge("g", "u", 15);
        graph.AddEdge("k", "a", 7);
        graph.AddEdge("k", "s", 10);
        graph.AddEdge("n", "f", 5);
        graph.AddEdge("n", "r", 6);
        graph.AddEdge("p", "w", 1);
        graph.AddEdge("p", "n", 4);
        graph.AddEdge("g", "t", 12);
        graph.AddEdge("n", "t", 3);
        graph.AddEdge("z", "k", 2);

        return graph;

    }
}
