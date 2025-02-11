using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class GraphTests
{
    [TestMethod]
    public void TestAddNode()
    {
        Graph graph = new Graph();
        graph.AddNode("A", 0, 0);
        graph.AddNode("B", 1, 1);

        // Ověření, že uzly byly úspěšně přidány
        Assert.IsTrue(graph.ContainsNode("A"));
        Assert.IsTrue(graph.ContainsNode("B"));
    }

    [TestMethod]
    public void TestAddEdge()
    {
        Graph graph = new Graph();
        graph.AddNode("A", 0, 0);
        graph.AddNode("B", 1, 1);
        graph.AddEdge("A", "B", 5);

        // Ověření, že hrana byla úspěšně přidána
        Assert.IsTrue(graph.HasEdge("A", "B"));
        Assert.AreEqual(5, graph.GetEdgeWeight("A", "B"));
    }
}
