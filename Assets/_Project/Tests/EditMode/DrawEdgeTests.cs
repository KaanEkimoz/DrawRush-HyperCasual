using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Tests.EditMode
{
    public sealed class DrawEdgeTests
    {
        private readonly List<GameObject> _spawned = new();

        private DrawPart NewPart(Vector3 position)
        {
            var go = new GameObject("DrawPart");
            go.transform.position = position;
            _spawned.Add(go);
            return go.AddComponent<DrawPart>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        [Test]
        public void Endpoints_ContainsAndOther()
        {
            DrawPart a = NewPart(Vector3.zero);
            DrawPart b = NewPart(new Vector3(2, 0, 0));
            DrawPart stranger = NewPart(new Vector3(9, 0, 0));
            var edge = new DrawEdge(a, b);

            Assert.IsTrue(edge.Contains(a));
            Assert.IsTrue(edge.Contains(b));
            Assert.IsFalse(edge.Contains(stranger));
            Assert.AreSame(b, edge.Other(a));
            Assert.AreSame(a, edge.Other(b));
            Assert.IsNull(edge.Other(stranger));
        }

        [Test]
        public void PaintFromA_AcrossWholeEdge_CompletesAndFiresOnce()
        {
            DrawPart a = NewPart(Vector3.zero);
            DrawPart b = NewPart(new Vector3(2, 0, 0));
            var edge = new DrawEdge(a, b);

            int fired = 0;
            edge.Completed += _ => fired++;

            edge.PaintFrom(a, 1f);
            Assert.IsTrue(edge.IsComplete);
            Assert.AreEqual(1, fired);

            // Painting again must not fire a second time.
            edge.PaintFrom(b, 0f);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void PaintFromBothEnds_MeetsInMiddle_Completes()
        {
            DrawPart a = NewPart(Vector3.zero);
            DrawPart b = NewPart(new Vector3(2, 0, 0));
            var edge = new DrawEdge(a, b);

            int fired = 0;
            edge.Completed += _ => fired++;

            edge.PaintFrom(a, 0.4f);   // low span [0, 0.4]
            edge.PaintFrom(b, 0.6f);   // high span [0.6, 1] — gap remains
            Assert.IsFalse(edge.IsComplete);
            Assert.AreEqual(0, fired);

            edge.PaintFrom(b, 0.4f);   // high span shrinks to 0.4, meets low
            Assert.IsTrue(edge.IsComplete);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void PaintFrom_NonEndpoint_IsNoOp()
        {
            DrawPart a = NewPart(Vector3.zero);
            DrawPart b = NewPart(new Vector3(2, 0, 0));
            DrawPart stranger = NewPart(new Vector3(9, 0, 0));
            var edge = new DrawEdge(a, b);

            edge.PaintFrom(stranger, 1f);
            Assert.IsFalse(edge.IsComplete);
            Assert.AreEqual(0f, edge.Fill.PaintedLow);
            Assert.AreEqual(1f, edge.Fill.PaintedHigh);
        }

        [Test]
        public void PointAt_LerpsBetweenEndpoints()
        {
            DrawPart a = NewPart(Vector3.zero);
            DrawPart b = NewPart(new Vector3(2, 0, 0));
            var edge = new DrawEdge(a, b);

            Assert.AreEqual(new Vector3(1, 0, 0), edge.PointAt(0.5f));
            Assert.AreEqual(Vector3.zero, edge.PointAt(0f));
            Assert.AreEqual(new Vector3(2, 0, 0), edge.PointAt(1f));
        }

        [Test]
        public void NoWaypoint_StaysStraight()
        {
            DrawPart a = NewPart(Vector3.zero);
            DrawPart b = NewPart(new Vector3(4, 0, 0));
            var edge = new DrawEdge(a, b);

            Assert.IsFalse(edge.IsArc);
            Assert.AreEqual(4f, edge.Length, 1e-4f);
            Assert.AreEqual(new Vector3(2, 0, 0), edge.PointAt(0.5f));
        }

        [Test]
        public void Waypoint_FormsCircularArcThroughAllThree()
        {
            // A(-1,0,0), B(1,0,0), waypoint(0,0,1): a semicircle of radius 1 bowing +Z.
            DrawPart a = NewPart(new Vector3(-1, 0, 0));
            DrawPart b = NewPart(new Vector3(1, 0, 0));
            var wp = NewPart(new Vector3(0, 0, 1));   // reuse DrawPart's transform as the waypoint
            var edge = new DrawEdge(a, b) { Waypoint = wp.transform };

            Assert.IsTrue(edge.IsArc);
            // Endpoints preserved.
            Assert.AreEqual(new Vector3(-1, 0, 0), edge.PointAt(0f), "A");
            Assert.AreEqual(new Vector3(1, 0, 0), edge.PointAt(1f), "B");
            // Midpoint passes through the waypoint.
            Vector3 mid = edge.PointAt(0.5f);
            Assert.AreEqual(0f, mid.x, 1e-3f);
            Assert.AreEqual(1f, mid.z, 1e-3f);
            // Arc length of a unit semicircle is π.
            Assert.AreEqual(Mathf.PI, edge.Length, 1e-2f);
        }

        [Test]
        public void CollinearWaypoint_FallsBackToStraight()
        {
            DrawPart a = NewPart(new Vector3(-1, 0, 0));
            DrawPart b = NewPart(new Vector3(1, 0, 0));
            var wp = NewPart(Vector3.zero);   // on the A–B line → degenerate
            var edge = new DrawEdge(a, b) { Waypoint = wp.transform };

            Assert.IsFalse(edge.IsArc);
            Assert.AreEqual(2f, edge.Length, 1e-4f);
            Assert.AreEqual(Vector3.zero, edge.PointAt(0.5f));
        }
    }
}
