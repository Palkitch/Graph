using System;

namespace Graph
{
    class Program
    {
        static void Main(string[] args)
        {
            DijkstraGraph<string, string, int> graph = new DijkstraGraph<string, string, int>();
            GridIndex<string> gridIndex = new GridIndex<string>(5, 5); // Nastavení velikosti buňky

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

            // Výpis gridu jako dvourozměrné pole
            var gridArray = gridIndex.ToArray();
            for (int j = 0; j < gridArray.GetLength(1); j++)
            {
                for (int i = 0; i < gridArray.GetLength(0); i++)
                {
                    Console.Write(gridArray[i, j] + "\t");
                }
                Console.WriteLine();
            }

            // Bodové vyhledávání
            var point = gridIndex.GetPoint(10, 10);
            Console.WriteLine($"Bod na souřadnicích (10, 10): {point}");

            // Intervalové vyhledávání
            var pointsInInterval = gridIndex.GetPointsInRegion(10, 20, 20, 50);
            Console.WriteLine("Body v intervalu (10, 20) až (20, 50):");
            foreach (var p in pointsInInterval)
            {
                Console.WriteLine(p);
            }
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
