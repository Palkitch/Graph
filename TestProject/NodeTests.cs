using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class NodeTests
{
    [TestMethod]
    public void TestNodeCreation()
    {
        Node node = new Node("A", 1.5, 2.5);

        Assert.AreEqual("A", node.Id);
        Assert.AreEqual(1.5, node.X);
        Assert.AreEqual(2.5, node.Y);
    }

    [TestMethod]
    public void TestAddNeighbor()
    {
        Node nodeA = new Node("A", 0, 0);
        Node nodeB = new Node("B", 1, 1);

        nodeA.AddNeighbor(nodeB, 5);

        Assert.IsTrue(nodeA.Neighbors.ContainsKey(nodeB));
        Assert.AreEqual(5, nodeA.Neighbors[nodeB]);
    }
}
