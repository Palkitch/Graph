internal class Program
{
    private static void Main(string[] args)
    {
        // Vytvoření instance grafu
        var graph = new GenGraph<string, string, int>();

        // Přidání vrcholů
        graph.AddVertex("A", "Město A");
        graph.AddVertex("B", "Město B");
        graph.AddVertex("C", "Město C");
        graph.AddVertex("D", "Město D");
        graph.AddVertex("E", "Město E");

        // Přidání hran (vzdáleností mezi městy)
        graph.AddEdge("A", "B", 4);
        graph.AddEdge("A", "C", 2);
        graph.AddEdge("B", "D", 3);
        graph.AddEdge("C", "D", 1);
        graph.AddEdge("C", "E", 5);
        graph.AddEdge("D", "E", 2);


        // Nalezení nejkratších cest z vrcholu "A"
        var shortestPaths = graph.FindShortestPaths("A");

        // Výpis výsledků
        Console.WriteLine("Nejkratší cesty z města A do ostatních měst:");
        foreach (var path in shortestPaths)
        {
            Console.WriteLine($"Do města {path.Key}: {path.Value} jednotek");
        }

        // Demonstrace dalších funkcí grafu
        Console.WriteLine("\nInformace o vrcholu B:");
        var vertexInfo = graph.FindVertex("B");
        Console.WriteLine($"Hodnota vrcholu B: {vertexInfo}");

        Console.WriteLine("\nOdstranění hrany mezi A a C:");
        bool removed = graph.RemoveEdge("A", "C");
        Console.WriteLine($"Hrana odstraněna: {removed}");

        // Opětovné nalezení nejkratších cest po odstranění hrany
        shortestPaths = graph.FindShortestPaths("A");
        Console.WriteLine("\nNejkratší cesty z města A po odstranění hrany A-C:");
        foreach (var path in shortestPaths)
        {
            Console.WriteLine($"Do města {path.Key}: {path.Value} jednotek");

        }

    }
}