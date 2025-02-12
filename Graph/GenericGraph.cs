using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Edge<KVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
{
    public KVertex From { get; }
    public KVertex To { get; }
    public VEdge Weight { get; set; }
    public bool IsAccessible { get; set; } = true;

    public Edge(KVertex from, KVertex to, VEdge weight)
    {
        From = from;
        To = to;
        Weight = weight;
    }
}

public class GenericGraph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
{
    private readonly Dictionary<KVertex, Vertex> vertices = new();
    public Dictionary<KVertex, KVertex> Predecessors { get; private set; }
    private class Vertex
    {
        public KVertex Key { get; }
        public VVertex Value { get; set; }
        public List<Edge<KVertex, VEdge>> Edges { get; } = new();

        public Vertex(KVertex key, VVertex value)
        {
            Key = key;
            Value = value;
        }
    }
    #region Edge/Vertex methods
    public bool AddVertex(KVertex key, VVertex value)
    {
        if (vertices.ContainsKey(key)) return false;
        vertices[key] = new Vertex(key, value);
        return true;
    }
    public bool AddEdge(KVertex from, KVertex to, VEdge weight)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to) || from.Equals(to)) return false;

        var edge = new Edge<KVertex, VEdge>(from, to, weight);
        vertices[from].Edges.Add(edge);
        vertices[to].Edges.Add(new Edge<KVertex, VEdge>(to, from, weight));
        return true;
    }
    public bool ChangeAccessibility(KVertex from, KVertex to)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to)) return false;

        var edgeFrom = vertices[from].Edges.FirstOrDefault(e => e.To.Equals(to));
        var edgeTo = vertices[to].Edges.FirstOrDefault(e => e.To.Equals(from));

        if (edgeFrom != null) edgeFrom.IsAccessible = !edgeFrom.IsAccessible;
        if (edgeTo != null) edgeTo.IsAccessible = !edgeTo.IsAccessible;

        return edgeFrom != null && edgeTo != null;
    }
    public bool RemoveEdge(KVertex from, KVertex to)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to)) return false;

        vertices[from].Edges.RemoveAll(e => e.To.Equals(to));
        vertices[to].Edges.RemoveAll(e => e.To.Equals(from));
        return true;
    }
    public VVertex? FindVertex(KVertex key) => vertices.TryGetValue(key, out var vertex) ? vertex.Value : default;
    private IEnumerable<Edge<KVertex, VEdge>> GetNeighbors(KVertex key) => vertices.TryGetValue(key, out var vertex) ? vertex.Edges.Where(e => e.IsAccessible) : Enumerable.Empty<Edge<KVertex, VEdge>>();
    #endregion
    #region Methods used in shortest path algorithm
    private VEdge GetMaxValue()
    {
        if (typeof(VEdge) == typeof(int)) return (VEdge)(object)int.MaxValue;
        if (typeof(VEdge) == typeof(double)) return (VEdge)(object)double.MaxValue;
        if (typeof(VEdge) == typeof(float)) return (VEdge)(object)float.MaxValue;
        throw new NotSupportedException("Unsupported edge type");
    }
    private VEdge Add(VEdge a, VEdge b) => a + b;
    private bool Less(VEdge a, VEdge b) => a.CompareTo(b) < 0;
    public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsFromVertex(KVertex startKey)
    {
        var distances = new Dictionary<KVertex, VEdge>();
        Predecessors = new Dictionary<KVertex, KVertex>();
        var visited = new HashSet<KVertex>();
        var priorityQueue = new PriorityQueue<KVertex, VEdge>();

        var maxValue = GetMaxValue();

        foreach (var vertex in vertices.Keys)
        {
            distances[vertex] = EqualityComparer<KVertex>.Default.Equals(vertex, startKey) ? default! : maxValue;
        }
        priorityQueue.Enqueue(startKey, default!);

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);

            foreach (var edge in GetNeighbors(current))
            {
                if (visited.Contains(edge.To)) continue;
                var newDistance = Add(distances[current], edge.Weight);
                if (Less(newDistance, distances[edge.To]))
                {
                    distances[edge.To] = newDistance;
                    Predecessors[edge.To] = current;
                    priorityQueue.Enqueue(edge.To, newDistance);
                }
            }
        }

        return BuildPaths(distances, startKey);
    }
    private Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> BuildPaths(Dictionary<KVertex, VEdge> distances, KVertex source)
    {
        var paths = new Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)>();
        var maxValue = GetMaxValue();

        foreach (var vertex in distances.Keys)
        {
            if (EqualityComparer<VEdge>.Default.Equals(distances[vertex], maxValue))
                continue; // Izolovaný vrchol ignorujeme

            var path = new List<KVertex>();
            var current = vertex;
            while (Predecessors.ContainsKey(current))
            {
                path.Add(current);
                current = Predecessors[current];
            }
            path.Add(source);
            path.Reverse();
            paths[vertex] = (distances[vertex], path);
        }
        return paths;
    }
    #endregion
    #region Print methods
    public void PrintGraph()
    {
        foreach (var vertex in vertices)
        {
            Console.Write($"{vertex.Key}: ");
            Console.WriteLine(string.Join(", ", vertex.Value.Edges.Select(e => $"{e.To} [{e.Weight}] {(e.IsAccessible ? "" : "(Blocked)")}")));
        }
    }
    public void PrintPredecessors()
    {
        int columnWidth = 4;

        var vertices = string.Join("", Predecessors.Keys.Select(k => k.ToString().PadRight(columnWidth)));
        var predecessors = string.Join("", Predecessors.Values.Select(v => v.ToString().PadRight(columnWidth)));

        Console.WriteLine("Vrcholy:    " + vertices);
        Console.WriteLine("Předchůdci: " + predecessors);
    }
    #endregion
}
