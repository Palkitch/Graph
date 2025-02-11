public class Path
{
    public List<Node> Nodes { get; private set; }
    public int TotalWeight { get; private set; }

    public Path()
    {
        Nodes = new List<Node>();
        TotalWeight = 0;
    }

    public void AddNode(Node node, int weight)
    {
        Nodes.Add(node);
        TotalWeight += weight;
    }
}
