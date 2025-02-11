public class Node
{
    public string Id { get; private set; }
    public double X { get; private set; }
    public double Y { get; private set; }
    public Dictionary<Node, int> Neighbors { get; private set; }

    public Node(string id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
        Neighbors = new Dictionary<Node, int>();
    }

    public void AddNeighbor(Node neighbor, int weight)
    {
        Neighbors[neighbor] = weight;
    }
}
