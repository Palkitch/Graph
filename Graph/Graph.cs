
public class Graph
{
    private Dictionary<string, Node> nodes;

    public Graph()
    {
        nodes = new Dictionary<string, Node>();
    }

    public void AddNode(string id, double x, double y)
    {
        nodes[id] = new Node(id, x, y);
    }

    public void AddEdge(string fromId, string toId, int weight)
    {
        nodes[fromId].AddNeighbor(nodes[toId], weight);
        nodes[toId].AddNeighbor(nodes[fromId], weight);
    }

    public bool ContainsNode(string id)
    {
        return nodes.ContainsKey(id);
    }

    public bool HasEdge(string fromId, string toId)
    {
        return nodes[fromId].Neighbors.ContainsKey(nodes[toId]);
    }

    public int GetEdgeWeight(string fromId, string toId)
    {
        return nodes[fromId].Neighbors[nodes[toId]];
    }
}
