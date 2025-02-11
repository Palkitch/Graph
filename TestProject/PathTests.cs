using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

[TestClass]
public class PathTests
{
    [TestMethod]
    public void TestPathCreation()
    {
        Path path = new Path();
        Assert.AreEqual(0, path.TotalWeight);
        Assert.AreEqual(0, path.Nodes.Count);
    }

    [TestMethod]
    public void TestAddNode()
    {
        Path path = new Path();
        Node nodeA = new Node("A", 0, 0);
        Node nodeB = new Node("B", 1, 1);

        path.AddNode(nodeA, 0);
        path.AddNode(nodeB, 5);

        Assert.AreEqual(2, path.Nodes.Count);
        Assert.AreEqual(5, path.TotalWeight);
    }
}
