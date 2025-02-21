using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
                path.Insert(0, vertex);
            }
            return path;
        }

        public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsFromVertex(KVertex startVertex)
        {
            this.startVertex = startVertex;
            Predecessors = new();
            var distances = InitializeDistances(startVertex);
            var paths = InitializePaths(startVertex);
            var visited = new HashSet<KVertex>();
            var priorityQueue = new PriorityQueue<KVertex, VEdge>();

            priorityQueue.Enqueue(startVertex, VEdge.Zero);

            while (priorityQueue.Count > 0)
            {
                var current = priorityQueue.Dequeue();
                if (visited.Contains(current)) continue;
                visited.Add(current);

                foreach (var edge in GetNeighbors(current))
                {
                    if (visited.Contains(edge.To)) continue;
                    var newDistance = distances[current] + edge.Weight;

                    if (newDistance.CompareTo(distances[edge.To]) < 0)
                    {
                        distances[edge.To] = newDistance;
                        Predecessors[edge.To] = current;
                        paths[edge.To] = new List<KVertex>(paths[current]) { edge.To };
                        priorityQueue.Enqueue(edge.To, newDistance);
                    }
                }
            }

            return BuildResult(distances, paths);
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
            catch (NullReferenceException)
            {
                Console.WriteLine("Pro zobrazení matice předchůdců je potřeba nejdřív spustit algoritmus pro nalezení nejkratší cesty");
            }
        }

        private Dictionary<KVertex, VEdge> InitializeDistances(KVertex startVertex)
        {
            var distances = new Dictionary<KVertex, VEdge>();
            var maxValue = GetMaxValue();

            foreach (var vertex in vertices.Keys)
            {
                distances[vertex] = EqualityComparer<KVertex>.Default.Equals(vertex, startVertex) ? VEdge.Zero : maxValue;
            }

            return distances;
        }

        private Dictionary<KVertex, List<KVertex>> InitializePaths(KVertex startVertex)
        {
            var paths = new Dictionary<KVertex, List<KVertex>>();

            foreach (var vertex in vertices.Keys)
            {
                paths[vertex] = new List<KVertex>();
            }

            paths[startVertex].Add(startVertex);
            return paths;
        }

        private Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> BuildResult(Dictionary<KVertex, VEdge> distances, Dictionary<KVertex, List<KVertex>> paths)
        {
            var result = new Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)>();
            var maxValue = GetMaxValue();

            foreach (var vertex in distances.Keys)
            {
                if (!EqualityComparer<VEdge>.Default.Equals(distances[vertex], maxValue))
                {
                    result[vertex] = (distances[vertex], paths[vertex]);
                }
            }

            return result;
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
            int columnWidth = 4;

            Console.Write("".PadRight(columnWidth));
            foreach (var colVertex in shortestPathsTable.Keys.Where(v => !v.Equals(startVertex)))
            {
                Console.Write(colVertex.ToString().PadRight(columnWidth));
            }
            Console.WriteLine();

            foreach (var row in shortestPathsTable)
            {
                Console.Write(row.Key.ToString().PadRight(columnWidth));
                foreach (var col in row.Value.Where(c => !c.Key.Equals(startVertex)))
                {
                    Console.Write(col.Value.PadRight(columnWidth));
                }
                Console.WriteLine();
            }
        }

        public void PrintShortestPathsTable(KVertex startKey)
        {
            var shortestPaths = FindShortestPathsFromVertex(startKey);
            var vertices = shortestPaths.Keys.OrderBy(k => !k.Equals(startKey)).ToList();
            int columnWidth = vertices.Max(v => v.ToString().Length) + 4;

            Console.Write("".PadRight(columnWidth));
            foreach (var vertex in vertices.Where(v => !v.Equals(startKey)))
            {
                Console.Write(vertex.ToString().PadRight(columnWidth));
            }
            Console.WriteLine();

            foreach (var rowVertex in vertices)
            {
                Console.Write(rowVertex.ToString().PadRight(columnWidth));
                foreach (var colVertex in vertices.Where(v => !v.Equals(startKey)))
                {
                    if (shortestPaths.ContainsKey(colVertex) && shortestPaths[colVertex].Path.Count > 1)
                    {
                        var path = shortestPaths[colVertex].Path;
                        int index = path.IndexOf(rowVertex);

                        if (index != -1 && index < path.Count - 1)
                        {
                            Console.Write(path[index + 1].ToString().PadRight(columnWidth));
                        }
                        else
                        {
                            Console.Write("".PadRight(columnWidth));
                        }
                    }
                    else
                    {
                        Console.Write("".PadRight(columnWidth));
                    }
                }
                Console.WriteLine();
            }
        }

        public Dictionary<KVertex, Dictionary<KVertex, string>> GetShortestPathsTable(KVertex startKey)
        {
            var shortestPaths = FindShortestPathsFromVertex(startKey);
            var vertices = shortestPaths.Keys.OrderBy(k => !k.Equals(startKey)).ToList();
            var table = new Dictionary<KVertex, Dictionary<KVertex, string>>();

            foreach (var rowVertex in vertices)
            {
                table[rowVertex] = new Dictionary<KVertex, string>();
                foreach (var colVertex in vertices)
                {
                    if (rowVertex.Equals(colVertex))
                    {
                        table[rowVertex][colVertex] = "";
                    }
                    else if (shortestPaths.ContainsKey(colVertex) && shortestPaths[colVertex].Path.Count > 1)
                    {
                        var path = shortestPaths[colVertex].Path;
                        int index = path.IndexOf(rowVertex);

                        if (index != -1 && index < path.Count - 1)
                        {
                            table[rowVertex][colVertex] = path[index + 1].ToString();
                        }
                        else
                        {
                            table[rowVertex][colVertex] = "";
                        }
                    }
                    else
                    {
                        table[rowVertex][colVertex] = "";
                    }
                }
            }

            return table;
        }
    }
}
