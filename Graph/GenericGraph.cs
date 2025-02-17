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
    public Dictionary<KVertex, KVertex> Predecessors { get; private set; } = new();

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

    public Dictionary<KVertex, (VEdge Distance, List<KVertex> Path)> FindShortestPathsFromVertex(KVertex startKey)
    {
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
        int columnWidth = 6;

        var vertices = string.Join("", Predecessors.Keys.Select(k => k.ToString().PadRight(columnWidth)));
        var predecessors = string.Join("", Predecessors.Values.Select(v => v.ToString().PadRight(columnWidth)));

        Console.WriteLine("Vrcholy:    " + vertices);
        Console.WriteLine("Předchůdci: " + predecessors);
    }

    
    public KVertex[,] GetSuccessorMatrix()
    {
        var vertexKeys = vertices.Keys.ToList();
        int n = vertexKeys.Count;
        var matrix = new KVertex[n, n];

        // Initialize matrix with default values
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                matrix[i, j] = default!;
            }
        }

        // Update the matrix with successors
        for (int i = 0; i < n; i++)
        {
            var shortestPaths = FindShortestPathsFromVertex(vertexKeys[i]);
            for (int j = 0; j < n; j++)
            {
                if (shortestPaths.TryGetValue(vertexKeys[j], out var pathInfo) && pathInfo.Path.Count > 1)
                {
                    matrix[i, j] = pathInfo.Path[1]; // The first step to get from i to j
                }
            }
        }

        return matrix;
    }

    public void PrintSuccessorMatrix()
    {
        var matrix = GetSuccessorMatrix();
        var vertexKeys = vertices.Keys.ToList();
        int n = vertexKeys.Count;

        Console.Write("     ");
        foreach (var key in vertexKeys)
        {
            Console.Write($"{key} ");
        }
        Console.WriteLine();

        for (int i = 0; i < n; i++)
        {
            Console.Write($"{vertexKeys[i]} ");
            for (int j = 0; j < n; j++)
            {
                Console.Write($"{matrix[i, j]} ");
            }
            Console.WriteLine();
        }
    }
    #endregion
}