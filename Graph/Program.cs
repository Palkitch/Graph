using System.Globalization;

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
        graph.AddEdge("D", "E", 4);
        graph.AddEdge("A", "E", 8);



        var result = graph.FindShortestPaths("A");

        graph.PrintPredecessors();
    }
}
