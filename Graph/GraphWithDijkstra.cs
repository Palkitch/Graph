using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Graph
{
    class GraphWithDijkstra<KVertex, VVertex, VEdge> : Graph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
    {
        public Dictionary<KVertex, KVertex> Predecessors { get; private set; }
        public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsFromVertex(KVertex startKey) // Do potomka
        {
            Predecessors = new();
            var distances = new Dictionary<KVertex, VEdge>(); // Stores the shortest distances from the start vertex
            var paths = new Dictionary<KVertex, List<KVertex>>(); // Stores the shortest paths for each vertex
            var visited = new HashSet<KVertex>(); // Set of visited vertices
            var priorityQueue = new PriorityQueue<KVertex, VEdge>(); // Priority queue for processing vertices by shortest distance

            var maxValue = GetMaxValue(); // Maximum possible value for the edge type

            // Initialize distances and paths
            foreach (var vertex in vertices.Keys)
            {
                distances[vertex] = EqualityComparer<KVertex>.Default.Equals(vertex, startKey) ? VEdge.Zero : maxValue;
                paths[vertex] = new List<KVertex>(); // Initialize an empty path
            }

            paths[startKey].Add(startKey); // The start vertex is its own path
            priorityQueue.Enqueue(startKey, VEdge.Zero);

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

            return result;
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

    }
}