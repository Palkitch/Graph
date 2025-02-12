
[TestClass]
public class GenGraphTests
{
    private GenericGraph<int, string, int> graph;

    [TestInitialize]
    public void SetUp()
    {
        graph = new GenericGraph<int, string, int>();
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
    [TestMethod]
    public void FindShortestPaths_SimpleGraph_ShouldReturnCorrectPath()
    {
        graph.AddVertex(1, "A");
        graph.AddVertex(2, "B");
        graph.AddEdge(1, 2, 5);

        var result = graph.FindShortestPathsFromVertex(1);

        Assert.IsTrue(result.ContainsKey(2));
        Assert.AreEqual(5, result[2].Distance);
        CollectionAssert.AreEqual(new List<int> { 1, 2 }, result[2].Path);
    }

    [TestMethod]
    public void FindShortestPaths_MultiplePaths_ShouldChooseShortest()
    {
        graph.AddVertex(1, "A");
        graph.AddVertex(2, "B");
        graph.AddVertex(3, "C");
        graph.AddEdge(1, 2, 10);
        graph.AddEdge(2, 3, 5);
        graph.AddEdge(1, 3, 8); // Přímá cesta je kratší

        var result = graph.FindShortestPathsFromVertex(1);

        Assert.AreEqual(8, result[3].Distance);
        CollectionAssert.AreEqual(new List<int> { 1, 3 }, result[3].Path);
    }

    [TestMethod]
    public void FindShortestPaths_UnreachableNode_ShouldHaveMaxDistance()
    {
        graph.AddVertex(1, "A");
        graph.AddVertex(2, "B");
        graph.AddVertex(3, "C"); // C není propojeno

        graph.AddEdge(1, 2, 5);

        var result = graph.FindShortestPathsFromVertex(1);

        Assert.IsFalse(result.ContainsKey(3)); // C nelze dosáhnout
    }

    [TestMethod]
    public void FindShortestPaths_SameNode_ShouldHaveZeroDistance()
    {
        graph.AddVertex(1, "A");

        var result = graph.FindShortestPathsFromVertex(1);

        Assert.AreEqual(0, result[1].Distance);
        CollectionAssert.AreEqual(new List<int> { 1 }, result[1].Path);
    }
    [TestMethod]
    public void FindShortestPaths_LargeGraph_ShouldPreferShorterPaths()
    {
        graph.AddVertex(1, "A");
        graph.AddVertex(2, "B");
        graph.AddVertex(3, "C");
        graph.AddVertex(4, "D");

        graph.AddEdge(1, 2, 10);
        graph.AddEdge(2, 3, 5);
        graph.AddEdge(1, 3, 3); // Kratší přímá cesta
        graph.AddEdge(3, 4, 2);
        graph.AddEdge(2, 4, 15);

        var result = graph.FindShortestPathsFromVertex(1);

        // Nejkratší cesta do 3 by měla být 3 (přímá hrana)
        Assert.AreEqual(3, result[3].Distance);
        CollectionAssert.AreEqual(new List<int> { 1, 3 }, result[3].Path);

        // Nejkratší cesta do 4 by měla být 5 (1 -> 3 -> 4)
        Assert.AreEqual(5, result[4].Distance);
        CollectionAssert.AreEqual(new List<int> { 1, 3, 4 }, result[4].Path);
    }

    [TestMethod]
    public void FindShortestPaths_IsolatedVertex_ShouldNotBeInResult()
    {
        graph.AddVertex(1, "A");
        graph.AddVertex(2, "B");
        graph.AddVertex(3, "C");
        graph.AddVertex(4, "D"); // Izolovaný vrchol

        graph.AddEdge(1, 2, 5);
        graph.AddEdge(2, 3, 7);

        var result = graph.FindShortestPathsFromVertex(1);

        Assert.IsTrue(result.ContainsKey(2));
        Assert.IsTrue(result.ContainsKey(3));
        Assert.IsFalse(result.ContainsKey(4)); // Vrchol 4 není dosažitelný
    }
    

}
