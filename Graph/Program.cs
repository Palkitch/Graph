using System;

namespace Graph
{
    class Program
    {
        static void Main(string[] args)
        {
            DijkstraGraph<string, string, int> graph = new DijkstraGraph<string, string, int>();
            GridIndex<string> gridIndex = new GridIndex<string>(70, 50); // Nastavení velikosti buňky

            // Přidání vrcholů do grafu a gridu
            AddVertex(graph, gridIndex, "Z", "Data Z", 5, 5);
            AddVertex(graph, gridIndex, "K", "Data K", 10, 10);
            AddVertex(graph, gridIndex, "U", "Data U", 15, 45);
            AddVertex(graph, gridIndex, "A", "Data A", 20, 25);
            AddVertex(graph, gridIndex, "W", "Data W", 22, 60);
            AddVertex(graph, gridIndex, "P", "Data P", 25, 55);
            AddVertex(graph, gridIndex, "R", "Data R", 28, 50);
            AddVertex(graph, gridIndex, "X", "Data X", 30, 30);
            AddVertex(graph, gridIndex, "N", "Data N", 33, 62);
            AddVertex(graph, gridIndex, "S", "Data S", 35, 12);
            AddVertex(graph, gridIndex, "F", "Data F", 38, 52);
            AddVertex(graph, gridIndex, "I", "Data I", 40, 20);
            AddVertex(graph, gridIndex, "T", "Data T", 42, 65);
            AddVertex(graph, gridIndex, "G", "Data G", 45, 40);
            AddVertex(graph, gridIndex, "M", "Data M", 47, 36);

            gridIndex.PrintXLines();
            gridIndex.PrintYLines();
        }

        static void AddVertex(DijkstraGraph<string, string, int> graph, GridIndex<string> gridIndex, string vertexId, string vertexData, int x, int y)
        {
            if (graph.AddVertex(vertexId, vertexData))
            {
                gridIndex.AddPoint(x, y, vertexId);
                Console.WriteLine($"Vrchol {vertexId} přidán na souřadnice ({x}, {y}).");
            }
            else
            {
                Console.WriteLine($"Vrchol {vertexId} již existuje.");
            }
        }
    }
}
