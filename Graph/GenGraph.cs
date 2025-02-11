using System;
using System.Collections.Generic;
using System.Numerics;

public class GenGraph<K, V, E> where E : IComparable<E>, INumber<E>
{
    private class Vertex
    {
        public K Key { get; }
        public V Value { get; set; }
        public Dictionary<K, E> Edges { get; } = new();

        public Vertex(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    private readonly Dictionary<K, Vertex> vertices = new();

    public bool AddVertex(K key, V value)
    {
        if (vertices.ContainsKey(key)) return false;
        vertices[key] = new Vertex(key, value);
        return true;
    }

    public bool AddEdge(K from, K to, E value)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to) || from.Equals(to)) return false;

        var v1 = vertices[from];
        var v2 = vertices[to];

        if (v1.Edges.ContainsKey(to)) return false; // Hrana už existuje

        v1.Edges[to] = value;
        v2.Edges[from] = value;
        return true;
    }

    public bool RemoveEdge(K from, K to)
    {
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to)) return false;

        var v1 = vertices[from];
        var v2 = vertices[to];

        if (!v1.Edges.ContainsKey(to)) return false;

        v1.Edges.Remove(to);
        v2.Edges.Remove(from);
        return true;
    }

    public V? FindVertex(K key)
    {
        return vertices.TryGetValue(key, out var vertex) ? vertex.Value : default;
    }

    private IEnumerable<KeyValuePair<K, E>> GetNeighbors(K key)
    {
        if (vertices.TryGetValue(key, out var vertex))
        {
            return vertex.Edges;
        }
        return Enumerable.Empty<KeyValuePair<K, E>>();
    }

    private E GetMaxValue()
    {
        if (typeof(E) == typeof(int)) return (E)(object)int.MaxValue;
        if (typeof(E) == typeof(double)) return (E)(object)double.MaxValue;
        if (typeof(E) == typeof(float)) return (E)(object)float.MaxValue;
        throw new NotSupportedException("Unsupported edge type");
    }

    private E Add(E a, E b)
    {
        return a + b; 
    }

    private bool Less(E a, E b)
    {
        return a.CompareTo(b) < 0;
    }

    public Dictionary<K, E> FindShortestPaths(K startKey)
    {
        var distances = new Dictionary<K, E>();
        var visited = new HashSet<K>();
        var pq = new PriorityQueue<K, E>();

        // Inicializace vzdáleností
        foreach (var vertex in vertices.Keys)
        {
            if (EqualityComparer<K>.Default.Equals(vertex, startKey))
            {
                distances[vertex] = default(E)!;
                pq.Enqueue(vertex, default(E)!);
            }
            else
            {
                distances[vertex] = GetMaxValue();
            }
        }

        while (pq.Count > 0)
        {
            var current = pq.Dequeue();
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
                    pq.Enqueue(neighbor.Key, newDistance);
                }
            }
        }

        return distances;
    }

}