using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    public class GraphWithDijkstra<KVertex, VVertex, VEdge> : Graph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
    {
        public Dictionary<KVertex, KVertex> Predecessors { get; private set; }
        private KVertex startVertex;
        public List<KVertex> GetPath(KVertex destination)
        {
            var path = new List<KVertex>();
            for (var vertex = destination; vertex != null; vertex = Predecessors.GetValueOrDefault(vertex))
            {
                path.Insert(0, vertex); // Insert at the beginning to reverse the order
            }
            return path;
        }

        public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsFromVertex(KVertex startVertex) // Do potomka
        {
            this.startVertex = startVertex;
            Predecessors = new();
            var distances = new Dictionary<KVertex, VEdge>(); // Stores the shortest distances from the start vertex
            var paths = new Dictionary<KVertex, List<KVertex>>(); // Stores the shortest paths for each vertex
            var visited = new HashSet<KVertex>(); // Set of visited vertices
            var priorityQueue = new PriorityQueue<KVertex, VEdge>(); // Priority queue for processing vertices by shortest distance

            var maxValue = GetMaxValue(); // Maximum possible value for the edge type

            // Initialize distances and paths
            foreach (var vertex in vertices.Keys)
            {
                distances[vertex] = EqualityComparer<KVertex>.Default.Equals(vertex, startVertex) ? VEdge.Zero : maxValue;
                paths[vertex] = new List<KVertex>(); // Initialize an empty path
            }

            paths[startVertex].Add(startVertex); // The start vertex is its own path
            priorityQueue.Enqueue(startVertex, VEdge.Zero);

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue(); // Remove the vertex with the shortest distance
                if (visited.Contains(current)) continue; // Skip if already visited
                visited.Add(current); // Mark as visited

                // Process all accessible neighbors of the current vertex
                foreach (var edge in GetNeighbors(current))
                {
                    if (visited.Contains(edge.To)) continue; // Skip if neighbor already visited
                    var newDistance = distances[current] + edge.Weight; // Calculate new distance through this vertex

                    // If the new distance is shorter than the known distance, update it
                    if (newDistance.CompareTo(distances[edge.To]) < 0)
                    {
                        distances[edge.To] = newDistance;
                        Predecessors[edge.To] = current; // Save the current vertex as the predecessor

                        // Build a new shortest path for this vertex
                        paths[edge.To] = new List<KVertex>(paths[current]) { edge.To };

                        priorityQueue.Enqueue(edge.To, newDistance); // Add the neighbor to the priority queue
                    }
                }
            }
            // Build the output structure combining distances and paths
            var result = new Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)>();
            foreach (var vertex in distances.Keys)
            {
                if (!EqualityComparer<VEdge>.Default.Equals(distances[vertex], maxValue)) // Ignore isolated vertices
                {
                    result[vertex] = (distances[vertex], paths[vertex]);
                }
            }

            return result; // co kdyz se jeden vrchol dostane do prioritni fronty znovu? 
        }
        public void PrintPredecessors()
        {
            try
            {
                int columnWidth = 4;

                var vertices = string.Join("", Predecessors.Keys.Select(k => k.ToString().PadRight(columnWidth)));
                var predecessors = string.Join("", Predecessors.Values.Select(v => v.ToString().PadRight(columnWidth)));

                Console.WriteLine("Vrcholy:    " + vertices);
                Console.WriteLine("Předchůdci: " + predecessors);

            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("Pro zobrazení matice předchůdců je potřeba nejdřív spustit algoritmus pro nalezení nejkratší cesty");
            }
        }
        private IEnumerable<Edge<KVertex, VEdge>> GetNeighbors(KVertex key) => vertices.TryGetValue(key, out var vertex) ? vertex.Edges.Where(e => e.IsAccessible) : Enumerable.Empty<Edge<KVertex, VEdge>>();
        private VEdge GetMaxValue()
        {
            if (typeof(VEdge) == typeof(int)) return (VEdge)(object)int.MaxValue;
            if (typeof(VEdge) == typeof(double)) return (VEdge)(object)double.MaxValue;
            if (typeof(VEdge) == typeof(float)) return (VEdge)(object)float.MaxValue;
            throw new NotSupportedException("Unsupported edge type");

        }
        public void Vypis()
        {
            PrintMatrix();
            Console.WriteLine();
            PrintPredecessors();
        }
        private void PrintMatrix()
        {
            var shortestPathsTable = GetShortestPathsTable(startVertex);
            // Print the table with headers
            int columnWidth = 4; // Adjust the padding width as needed

            // Print the column headers (excluding startVertex column)
            Console.Write("".PadRight(columnWidth)); // Empty corner space
            foreach (var colVertex in shortestPathsTable.Keys.Where(v => !v.Equals(startVertex)))
            {
                Console.Write(colVertex.ToString().PadRight(columnWidth)); // Column headers
            }
            Console.WriteLine();

            // Print the table rows
            foreach (var row in shortestPathsTable)
            {
                // Print the row header (the vertex name)
                Console.Write(row.Key.ToString().PadRight(columnWidth));

                // Print the data for each column in the current row (excluding 'x' column)
                foreach (var col in row.Value.Where(c => !c.Key.Equals(startVertex)))
                {
                    Console.Write(col.Value.PadRight(columnWidth)); // Data cell
                }

                Console.WriteLine(); // Newline after each row
            }
        }
        public void PrintShortestPathsTable(KVertex startKey)
        {
            var shortestPaths = FindShortestPathsFromVertex(startKey);
            var vertices = shortestPaths.Keys.OrderBy(k => !k.Equals(startKey)).ToList(); // Ensures startKey is first
            int columnWidth = vertices.Max(v => v.ToString().Length) + 4; // Dynamická šířka sloupce

            // Hlavička tabulky
            Console.Write("".PadRight(columnWidth)); // Prázdné místo pro první buňku
            foreach (var vertex in vertices.Where(v => !v.Equals(startKey)))
            {
                Console.Write(vertex.ToString().PadRight(columnWidth));
            }
            Console.WriteLine();

            // Obsah tabulky
            foreach (var rowVertex in vertices)
            {
                Console.Write(rowVertex.ToString().PadRight(columnWidth));
                foreach (var colVertex in vertices.Where(v => !v.Equals(startKey)))
                {
                    if (shortestPaths.ContainsKey(colVertex) && shortestPaths[colVertex].Path.Count > 1)
                    {
                        var path = shortestPaths[colVertex].Path;
                        int index = path.IndexOf(rowVertex);

                        // Pokud je aktuální vrchol součástí cesty a není poslední
                        if (index != -1 && index < path.Count - 1)
                        {
                            Console.Write(path[index + 1].ToString().PadRight(columnWidth));
                        }
                        else
                        {
                            Console.Write("".PadRight(columnWidth)); // Prázdná buňka
                        }
                    }
                    else
                    {
                        Console.Write("".PadRight(columnWidth)); // Prázdná buňka
                    }
                }
                Console.WriteLine();
            }
        }
        public Dictionary<KVertex, Dictionary<KVertex, string>> GetShortestPathsTable(KVertex startKey)
        {
            var shortestPaths = FindShortestPathsFromVertex(startKey);
            var vertices = shortestPaths.Keys.OrderBy(k => !k.Equals(startKey)).ToList(); // Ensures startKey is first

            var table = new Dictionary<KVertex, Dictionary<KVertex, string>>();

            // Initialize the table with empty dictionaries
            foreach (var rowVertex in vertices)
            {
                table[rowVertex] = new Dictionary<KVertex, string>();
                foreach (var colVertex in vertices)
                {
                    if (rowVertex.Equals(colVertex))
                    {
                        table[rowVertex][colVertex] = ""; // No path to itself, empty string
                    }
                    else if (shortestPaths.ContainsKey(colVertex) && shortestPaths[colVertex].Path.Count > 1)
                    {
                        var path = shortestPaths[colVertex].Path;
                        int index = path.IndexOf(rowVertex);

                        // If the current vertex is part of the path and isn't the last element
                        if (index != -1 && index < path.Count - 1)
                        {
                            table[rowVertex][colVertex] = path[index + 1].ToString();
                        }
                        else
                        {
                            table[rowVertex][colVertex] = ""; // Empty string if there's no valid path part
                        }
                    }
                    else
                    {
                        table[rowVertex][colVertex] = ""; // Empty string for unreachable vertices
                    }
                }
            }

            return table;
        }

    }
}