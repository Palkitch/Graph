using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Graph
{
    public class DijkstraGraph<KVertex, VVertex, VEdge> : Graph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
    {
        public Dictionary<KVertex, KVertex> Predecessors { get; private set; }
        private KVertex startKey;
        private Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> shortestPaths;


        public List<KVertex> GetPath(KVertex destination)
        {
            var path = new List<KVertex>();
            for (var vertex = destination; vertex != null; vertex = Predecessors.GetValueOrDefault(vertex))
            {
                path.Insert(0, vertex);
            }
            return path;
        }

        public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPaths(KVertex startKey)
        {
            this.startKey = startKey;
            Predecessors = new();
            var distances = InitializeDistances(startKey);
            var paths = InitializePaths(startKey);
            var visited = new HashSet<KVertex>();
            var priorityQueue = new PriorityQueue<KVertex, VEdge>();

            priorityQueue.Enqueue(startKey, VEdge.Zero);

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

            shortestPaths = BuildResult(distances, paths);
            return shortestPaths;
        }

        public string PrintPredecessors()
        {
            if (Predecessors == null || Predecessors.Count == 0)
            {
                return "Pro zobrazení matice předchůdců je potřeba nejdřív spustit algoritmus pro nalezení nejkratší cesty";
            }

            var startVertex = Predecessors.Keys.FirstOrDefault();
            if (startVertex == null)
            {
                return "Pro zobrazení matice předchůdců je potřeba nejdřív spustit algoritmus pro nalezení nejkratší cesty";
            }

            int columnWidth = 4;
            var vertices = string.Join("", Predecessors.Keys.Select(k => k.ToString().PadRight(columnWidth)));
            var predecessors = string.Join("", Predecessors.Values.Select(v => v.ToString().PadRight(columnWidth)));

            var result = new System.Text.StringBuilder();
            result.AppendLine("Z:    " + vertices);
            result.AppendLine($"Do {startKey}: " + predecessors);

            return result.ToString();
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

        public string PrintShortestPathsTable()
        {
            //pokud se neprovedlo findShortest paths, tak tatot metoda upozorní uživatele a neprovede se 
            if (shortestPaths == null || shortestPaths.Count == 0)
            {
                return "Pro zobrazení tabulky nejkratších cest je potřeba nejdřív spustit algoritmus pro nalezení nejkratší cesty";
            }
            var vertices = shortestPaths.Keys.OrderBy(k => !k.Equals(startKey)).ToList();
            int columnWidth = vertices.Max(v => v.ToString().Length) + 4;
            var result = new System.Text.StringBuilder();

            result.Append("do".PadRight(columnWidth));
            foreach (var vertex in vertices.Where(v => !v.Equals(startKey)))
            {
                result.Append(vertex.ToString().PadRight(columnWidth));
            }
            result.AppendLine("\n-------------------------------------------------------------------------");

            foreach (var rowVertex in vertices)
            {
                result.Append(("z " + rowVertex.ToString()).PadRight(columnWidth));
                foreach (var colVertex in vertices.Where(v => !v.Equals(startKey)))
                {
                    if (shortestPaths.ContainsKey(colVertex) && shortestPaths[colVertex].Path.Count > 1)
                    {
                        var path = shortestPaths[colVertex].Path;
                        int index = path.IndexOf(rowVertex);

                        if (index != -1 && index < path.Count - 1)
                        {
                            result.Append(path[index + 1].ToString().PadRight(columnWidth));
                        }
                        else
                        {
                            result.Append("-".PadRight(columnWidth));
                        }
                    }
                    else
                    {
                        result.Append("-".PadRight(columnWidth));
                    }
                }
                result.AppendLine();
            }

            return result.ToString();
        }

    }
}
