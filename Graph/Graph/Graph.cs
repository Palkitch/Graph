using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Graph<KVertex, VVertex, VEdge> where VEdge : IComparable<VEdge>, INumber<VEdge>
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
    public List<KVertex> GetVertices()
    {
        return vertices.Keys.ToList();
    }
    public List<string> GetEdgesAsString()
    {
        var edges = new List<string>();
        var printedEdges = new HashSet<(KVertex, KVertex)>();

        foreach (var vertex in vertices)
        {
            foreach (var edge in vertex.Value.Edges)
            {
                var edgePair = (edge.From, edge.To);
                var reverseEdgePair = (edge.To, edge.From);

                if (!printedEdges.Contains(edgePair) && !printedEdges.Contains(reverseEdgePair))
                {
                    edges.Add($"{edge.From} <-> {edge.To}");
                    printedEdges.Add(edgePair);
                }
            }
        }

        return edges;
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
    public string PrintGraph()
    {
        var result = new System.Text.StringBuilder();
        var printedEdges = new HashSet<(KVertex, KVertex)>();

        // Nejprve vypiš vrcholy
        result.AppendLine("Vrcholy:");
        foreach (var vertex in vertices)
        {
            result.AppendLine($"{vertex.Key}: {vertex.Value.Value}");
        }

        // Poté vypiš hrany
        result.AppendLine("Hrany:");
        foreach (var vertex in vertices)
        {
            foreach (var edge in vertex.Value.Edges)
            {
                var edgePair = (edge.From, edge.To);
                var reverseEdgePair = (edge.To, edge.From);

                if (!printedEdges.Contains(edgePair) && !printedEdges.Contains(reverseEdgePair))
                {
                    result.AppendLine($"{edge.From} <-> {edge.To} [{edge.Weight}] {(edge.IsAccessible ? "" : "(Blocked)")}");
                    printedEdges.Add(edgePair);
                }
            }
        }

        return result.ToString();
    }
    public string PrintRawGraph()
    {
        var result = new System.Text.StringBuilder();
        var printedEdges = new HashSet<(KVertex, KVertex)>();

        // Nejprve vypiš vrcholy
        foreach (var vertex in vertices)
        {
            result.AppendLine($"V,{vertex.Key},{vertex.Value.Value}");
        }

        // Poté vypiš hrany
        foreach (var vertex in vertices)
        {
            foreach (var edge in vertex.Value.Edges)
            {
                var edgePair = (edge.From, edge.To);
                var reverseEdgePair = (edge.To, edge.From);

                if (!printedEdges.Contains(edgePair) && !printedEdges.Contains(reverseEdgePair))
                {
                    result.AppendLine($"E,{edge.From},{edge.To},{edge.Weight},{edge.IsAccessible}");
                    printedEdges.Add(edgePair);
                }
            }
        }

        return result.ToString();
    }

}
#endregion
