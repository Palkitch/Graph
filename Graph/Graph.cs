using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public abstract class Graph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
{
    protected readonly Dictionary<KVertex, Vertex> vertices = new();
    protected class Edge<KVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
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
    protected class Vertex
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
        if (!vertices.ContainsKey(from) || !vertices.ContainsKey(to) || from.Equals(to))
            return false;

        var existingEdgeFrom = vertices[from].Edges.FirstOrDefault(e => e.To.Equals(to));
        var existingEdgeTo = vertices[to].Edges.FirstOrDefault(e => e.To.Equals(from));

        if (existingEdgeFrom != null && existingEdgeTo != null)
        {
            existingEdgeFrom.Weight = weight;
            existingEdgeTo.Weight = weight;
            return true;
        }

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

    #endregion
}