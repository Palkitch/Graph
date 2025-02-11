
[TestClass]
public class GenGraphTests
{
    private GenGraph<int, string, int> graph;

    [TestInitialize]
    public void SetUp()
    {
        graph = new GenGraph<int, string, int>();
    }

    [TestMethod]
    public void AddVertex_ShouldAddVertex()
    {
        Assert.IsTrue(graph.AddVertex(1, "A"));
        Assert.AreEqual("A", graph.FindVertex(1));
    }

    [TestMethod]
    public void AddVertex_DuplicateKey_ShouldReturnFalse()
    {
        graph.AddVertex(1, "A");
        Assert.IsFalse(graph.AddVertex(1, "B"));
    }

    [TestMethod]
    public void AddEdge_ShouldAddEdge()
    {
        graph.AddVertex(1, "A");
        graph.AddVertex(2, "B");
        Assert.IsTrue(graph.AddEdge(1, 2, 10));
    }
}
