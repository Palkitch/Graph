using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

public class GenericGraph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
{
    private class Vertex
    {
        public KVertex Key { get; }
        public VVertex Value { get; set; }
        public Dictionary<KVertex, VEdge> Edges { get; } = new();

        public Vertex(KVertex key, VVertex value)
        {
            Key = key;
            Value = value;
        }
    }
    public Dictionary<KVertex, KVertex> Predecessors { get; private set; }
    private readonly Dictionary<KVertex, Vertex> vertices = new();

    public bool AddVertex(KVertex key, VVertex value)
    {
        if (vertices.ContainsKey(key)) return false;
        vertices[key] = new Vertex(key, value);
        return true;
    }

    public bool AddEdge(KVertex from, KVertex to, VEdge value)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to) || from.Equals(to)) return false; // Vrcholy u kterých chceme vytvořit hranu neexistují

        var v1 = vertices[from];
        var v2 = vertices[to];

        if (v1.Edges.ContainsKey(to)) return false; // Hrana už existuje

        v1.Edges[to] = value;
        v2.Edges[from] = value;
        return true;
    }

    public bool RemoveEdge(KVertex from, KVertex to)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to)) return false; // Kontrola, zda vrcholy vůbec existují

        var v1 = vertices[from];
        var v2 = vertices[to];

        if (!v1.Edges.ContainsKey(to)) return false;

        v1.Edges.Remove(to);
        v2.Edges.Remove(from);
        return true;
    }

    public VVertex? FindVertex(KVertex key)
    {
        return vertices.TryGetValue(key, out var vertex) ? vertex.Value : default;
    }

    private IEnumerable<KeyValuePair<KVertex, VEdge>> GetNeighbors(KVertex key)
    {
        if (vertices.TryGetValue(key, out var vertex))
        {
            return vertex.Edges;
        }
        return Enumerable.Empty<KeyValuePair<KVertex, VEdge>>();
    }

    private VEdge GetMaxValue()
    {
        if (typeof(VEdge) == typeof(int)) return (VEdge)(object)int.MaxValue;
        if (typeof(VEdge) == typeof(double)) return (VEdge)(object)double.MaxValue;
        if (typeof(VEdge) == typeof(float)) return (VEdge)(object)float.MaxValue;
        throw new NotSupportedException("Unsupported edge type");
    }

    private VEdge Add(VEdge a, VEdge b)
    {
        return a + b;
    }

    private bool Less(VEdge a, VEdge b)
    {
        return a.CompareTo(b) < 0;
    }

    public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsFromVertex(KVertex startKey)
    {
        var distances = new Dictionary<KVertex, VEdge>();
        Predecessors = new Dictionary<KVertex, KVertex>();
        var visited = new HashSet<KVertex>();
        var priorityQueue = new PriorityQueue<KVertex, VEdge>();

        var maxValue = GetMaxValue();

        // Inicializace vzdáleností a priority queue
        foreach (var vertex in vertices.Keys)
        {
            if (EqualityComparer<KVertex>.Default.Equals(vertex, startKey))
            {
                distances[vertex] = default(VEdge)!;
                priorityQueue.Enqueue(vertex, default(VEdge)!);
            }
            else
            {
                distances[vertex] = maxValue;
            }
        }

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (visited.Contains(neighbor.Key))
                    continue;

                var newDistance = Add(distances[current], neighbor.Value);
                if (Less(newDistance, distances[neighbor.Key]))
                {
                    distances[neighbor.Key] = newDistance;
                    Predecessors[neighbor.Key] = current;
                    priorityQueue.Enqueue(neighbor.Key, newDistance);
                }
            }
        }

        var paths = new Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)>();
        foreach (var vertex in distances.Keys)
        {
            if (EqualityComparer<VEdge>.Default.Equals(distances[vertex], maxValue))
                continue;

            var path = new List<KVertex>();
            var current = vertex;
            while (Predecessors.ContainsKey(current))
            {
                path.Add(current);
                current = Predecessors[current];
            }
            path.Add(startKey);
            path.Reverse();
            paths[vertex] = (distances[vertex], path);
        }

        return paths;
    }

    public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsToVertex(KVertex targetKey)
    {
        var distances = new Dictionary<KVertex, VEdge>();
        Predecessors = new Dictionary<KVertex, KVertex>();
        var visited = new HashSet<KVertex>();
        var priorityQueue = new PriorityQueue<KVertex, VEdge>();

        var maxValue = GetMaxValue();


        foreach (var vertex in vertices.Keys)
        {
            distances[vertex] = maxValue;
        }

        distances[targetKey] = default(VEdge)!;
        priorityQueue.Enqueue(targetKey, default(VEdge)!);

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            if (visited.Contains(current))
                continue;

            visited.Add(current);


            foreach (var neighbor in vertices)
            {
                if (!neighbor.Value.Edges.ContainsKey(current) || visited.Contains(neighbor.Key))
                    continue;

                var weight = neighbor.Value.Edges[current];
                var newDistance = Add(distances[current], weight);

                if (Less(newDistance, distances[neighbor.Key]))
                {
                    distances[neighbor.Key] = newDistance;
                    Predecessors[neighbor.Key] = current;
                    priorityQueue.Enqueue(neighbor.Key, newDistance);
                }
            }
        }

        var pathsToTarget = new Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)>();

        foreach (var vertex in distances.Keys)
        {
            if (EqualityComparer<VEdge>.Default.Equals(distances[vertex], maxValue))
                continue;

            var path = new List<KVertex>();
            var current = vertex;
            while (Predecessors.ContainsKey(current))
            {
                path.Add(current);
                current = Predecessors[current];
            }
            path.Add(targetKey);
            path.Reverse();

            pathsToTarget[vertex] = (distances[vertex], path);
        }

        return pathsToTarget;
    }

    public void PrintGraph()
    {
        foreach (var vertex in vertices)
        {
            Console.Write($"{vertex.Key}: ");
            Console.WriteLine(string.Join(", ", vertex.Value.Edges.Select(e => $"{e.Key} [{e.Value}]")));
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
}