using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Numerics;

public class GenGraph<VertexKey, VertexValue, EdgeValue> where EdgeValue : IComparable<EdgeValue>, INumber<EdgeValue>
{
    private class Vertex
    {
        public VertexKey Key { get; }
        public VertexValue Value { get; set; }
        public Dictionary<VertexKey, EdgeValue> Edges { get; } = new();

        public Vertex(VertexKey key, VertexValue value)
        {
            Key = key;
            Value = value;
        }
    }
    public Dictionary<VertexKey, VertexKey> Predecessors { get; private set; }
    private readonly Dictionary<VertexKey, Vertex> vertices = new();

    public bool AddVertex(VertexKey key, VertexValue value)
    {
        if (vertices.ContainsKey(key)) return false;
        vertices[key] = new Vertex(key, value);
        return true;
    }

    public bool AddEdge(VertexKey from, VertexKey to, EdgeValue value)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to) || from.Equals(to)) return false; // Vrcholy u kterých chceme vytvořit hranu neexistují

        var v1 = vertices[from];
        var v2 = vertices[to];

        if (v1.Edges.ContainsKey(to)) return false; // Hrana už existuje

        v1.Edges[to] = value;
        v2.Edges[from] = value;
        return true;
    }

    public bool RemoveEdge(VertexKey from, VertexKey to)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to)) return false; // Kontrola, zda vrcholy vůbec existují

        var v1 = vertices[from];
        var v2 = vertices[to];

        if (!v1.Edges.ContainsKey(to)) return false;

        v1.Edges.Remove(to);
        v2.Edges.Remove(from);
        return true;
    }

    public VertexValue? FindVertex(VertexKey key)
    {
        return vertices.TryGetValue(key, out var vertex) ? vertex.Value : default;
    }

    private IEnumerable<KeyValuePair<VertexKey, EdgeValue>> GetNeighbors(VertexKey key)
    {
        if (vertices.TryGetValue(key, out var vertex))
        {
            return vertex.Edges;
        }
        return Enumerable.Empty<KeyValuePair<VertexKey, EdgeValue>>();
    }

    private EdgeValue GetMaxValue()
    {
        if (typeof(EdgeValue) == typeof(int)) return (EdgeValue)(object)int.MaxValue;
        if (typeof(EdgeValue) == typeof(double)) return (EdgeValue)(object)double.MaxValue;
        if (typeof(EdgeValue) == typeof(float)) return (EdgeValue)(object)float.MaxValue;
        throw new NotSupportedException("Unsupported edge type");
    }

    private EdgeValue Add(EdgeValue a, EdgeValue b)
    {
        return a + b;
    }

    private bool Less(EdgeValue a, EdgeValue b)
    {
        return a.CompareTo(b) < 0;
    }

    public Dictionary<VertexKey, (EdgeValue Distance, List<VertexKey> Path)> FindShortestPaths(VertexKey startKey)
    {
        var distances = new Dictionary<VertexKey, EdgeValue>();
        Predecessors = new Dictionary<VertexKey, VertexKey>();
        var visited = new HashSet<VertexKey>();
        var priorityQueue = new PriorityQueue<VertexKey, EdgeValue>();

        var maxValue = GetMaxValue();

        // Inicializace vzdáleností a priority queue
        foreach (var vertex in vertices.Keys)
        {
            if (EqualityComparer<VertexKey>.Default.Equals(vertex, startKey))
            {
                distances[vertex] = default(EdgeValue)!;
                priorityQueue.Enqueue(vertex, default(EdgeValue)!);
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

        // Rekonstrukce cest s vynecháním nedosažitelných vrcholů
        var paths = new Dictionary<VertexKey, (EdgeValue Distance, List<VertexKey> Path)>();
        foreach (var vertex in distances.Keys)
        {
            if (EqualityComparer<EdgeValue>.Default.Equals(distances[vertex], maxValue))
                continue; // Nedosažitelný vrchol neukládáme

            var path = new List<VertexKey>();
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
    public void PrintPredecessors()
    {
        int columnWidth = 4;

        var vertices = string.Join("", Predecessors.Keys.Select(k => k.ToString().PadRight(columnWidth)));
        var predecessors = string.Join("", Predecessors.Values.Select(v => v.ToString().PadRight(columnWidth)));

        Console.WriteLine("Vrcholy:    " + vertices);
        Console.WriteLine("Předchůdci: " + predecessors);
    }
}