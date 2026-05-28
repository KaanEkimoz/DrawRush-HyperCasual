using NUnit.Framework;
using UnityEngine;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Tests.EditMode
{
    public sealed class DrawPartNeighborGraphTests
    {
        [Test]
        public void SinglePoint_ReturnsEmptyNeighborList()
        {
            var positions = new[] { Vector3.zero };
            var graph = DrawPartNeighborGraph.ComputeNearestNeighbors(positions, k: 2);
            Assert.AreEqual(1, graph.Length);
            Assert.IsEmpty(graph[0]);
        }

        [Test]
        public void TwoPoints_EachHasOneNeighbor_TheOther()
        {
            var positions = new[] { new Vector3(0, 0, 0), new Vector3(5, 0, 0) };
            var graph = DrawPartNeighborGraph.ComputeNearestNeighbors(positions, k: 2);
            Assert.AreEqual(new[] { 1 }, graph[0]);
            Assert.AreEqual(new[] { 0 }, graph[1]);
        }

        [Test]
        public void SquareCorners_NearestTwoAreEdgeNeighborsNotDiagonals()
        {
            // (0,0) (1,0) (1,1) (0,1) — adjacent edges length 1, diagonals length √2
            var positions = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 0),
                new Vector3(1, 0, 1),
                new Vector3(0, 0, 1),
            };
            var graph = DrawPartNeighborGraph.ComputeNearestNeighbors(positions, k: 2);
            // Corner 0: neighbors should be {1, 3}, not {2}
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, graph[0]);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, graph[1]);
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, graph[2]);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, graph[3]);
        }

        [Test]
        public void TriangleCorners_EachHasOtherTwoAsNeighbors()
        {
            var positions = new[]
            {
                new Vector3(0, 0, 0),
                new Vector3(2, 0, 0),
                new Vector3(1, 0, 1.732f), // equilateral height ~ √3
            };
            var graph = DrawPartNeighborGraph.ComputeNearestNeighbors(positions, k: 2);
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, graph[0]);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, graph[1]);
            CollectionAssert.AreEquivalent(new[] { 0, 1 }, graph[2]);
        }

        [Test]
        public void HexCorners_NearestTwoAreAdjacentNotAcross()
        {
            // Regular hexagon, radius 1, vertices at 60° intervals on XZ plane
            const float h = 0.8660254f; // sqrt(3)/2
            var positions = new[]
            {
                new Vector3( 1.0f, 0,  0f),
                new Vector3( 0.5f, 0,  h),
                new Vector3(-0.5f, 0,  h),
                new Vector3(-1.0f, 0,  0f),
                new Vector3(-0.5f, 0, -h),
                new Vector3( 0.5f, 0, -h),
            };
            var graph = DrawPartNeighborGraph.ComputeNearestNeighbors(positions, k: 2);
            // Each vertex should pair with its two arc-adjacent vertices.
            CollectionAssert.AreEquivalent(new[] { 1, 5 }, graph[0]);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, graph[1]);
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, graph[2]);
            CollectionAssert.AreEquivalent(new[] { 2, 4 }, graph[3]);
            CollectionAssert.AreEquivalent(new[] { 3, 5 }, graph[4]);
            CollectionAssert.AreEquivalent(new[] { 4, 0 }, graph[5]);
        }

        [Test]
        public void BuildUndirectedPairs_TwoPoints_OneEdge()
        {
            var adjacency = new[] { new[] { 1 }, new[] { 0 } };
            var pairs = DrawPartNeighborGraph.BuildUndirectedPairs(adjacency);
            Assert.AreEqual(new[] { (0, 1) }, pairs);
        }

        [Test]
        public void BuildUndirectedPairs_Square_FourEdgesNoDiagonals()
        {
            // Square neighbor graph: each corner pairs with its two edge-neighbors.
            var adjacency = new[]
            {
                new[] { 1, 3 },
                new[] { 0, 2 },
                new[] { 1, 3 },
                new[] { 0, 2 },
            };
            var pairs = DrawPartNeighborGraph.BuildUndirectedPairs(adjacency);
            Assert.AreEqual(new[] { (0, 1), (0, 3), (1, 2), (2, 3) }, pairs);
        }

        [Test]
        public void BuildUndirectedPairs_Triangle_ThreeEdges()
        {
            var adjacency = new[]
            {
                new[] { 1, 2 },
                new[] { 0, 2 },
                new[] { 0, 1 },
            };
            var pairs = DrawPartNeighborGraph.BuildUndirectedPairs(adjacency);
            Assert.AreEqual(new[] { (0, 1), (0, 2), (1, 2) }, pairs);
        }

        [Test]
        public void BuildUndirectedPairs_AsymmetricAdjacency_StillYieldsEdge()
        {
            // Only one side lists the neighbor; the edge must still appear once.
            var adjacency = new[] { new[] { 1 }, System.Array.Empty<int>() };
            var pairs = DrawPartNeighborGraph.BuildUndirectedPairs(adjacency);
            Assert.AreEqual(new[] { (0, 1) }, pairs);
        }

        [Test]
        public void BuildUndirectedPairs_DedupesRepeatsAndSkipsInvalid()
        {
            // Duplicate (0,1), self-loop, and an out-of-range index all collapse to one edge.
            var adjacency = new[] { new[] { 1, 1, 0, 99 }, new[] { 0 } };
            var pairs = DrawPartNeighborGraph.BuildUndirectedPairs(adjacency);
            Assert.AreEqual(new[] { (0, 1) }, pairs);
        }
    }
}
