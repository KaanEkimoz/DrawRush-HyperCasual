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

        [Test]
        public void Waypoint_BeyondCentre_TakesTheMajorArc()
        {
            // The sweep picks short-way vs long-way round the circle. Here the only path from A
            // to B that actually passes through the waypoint is the LONG one (~270°): the short
            // 90° hop would cut under the circle and miss it entirely. Getting this branch wrong
            // is what sends the player around the outside of a heart lobe instead of along it.
            // A(-0.5,0) B(0.5,0) W(0,2). Circumcentre (0, 0.9375), R = 1.0625. Let θ =
            // atan(1.875): A sits at -(π-θ), B at -θ, the waypoint at +π/2. Going A→B the direct
            // way is only π-2θ ≈ 0.98 rad (~56°) and misses the waypoint, so the arc must take
            // the rest of the circle: 2π-(π-2θ) = π+2θ ≈ 5.303 rad (~304°).
            DrawPart a = NewPart(new Vector3(-0.5f, 0, 0));
            DrawPart b = NewPart(new Vector3(0.5f, 0, 0));
            var wp = NewPart(new Vector3(0, 0, 2f));
            var edge = new DrawEdge(a, b) { Waypoint = wp.transform };

            Assert.IsTrue(edge.IsArc);
            Vector3 mid = edge.PointAt(0.5f);
            Assert.AreEqual(0f, mid.x, 1e-2f, "major-arc midpoint x");
            Assert.AreEqual(2f, mid.z, 1e-2f, "midpoint must land ON the waypoint, not the short way round");

            const float radius = 1.0625f;
            float sweep = Mathf.PI + 2f * Mathf.Atan(1.875f);
            Assert.AreEqual(radius * sweep, edge.Length, 5e-2f, "length must be the major arc");
            Assert.Greater(edge.Length, radius * Mathf.PI, "a major arc is longer than a semicircle");
        }

        [Test]
        public void TangentAt_RunsFromAtowardsB_AndIsUnitLength()
        {
            // Same semicircle bowing +Z. At the start the tangent must head +Z (up and over),
            // at the end it must head -Z (coming back down) — i.e. it follows A→B, and the sign
            // is what RailPaintController uses to convert stick input into progress.
            DrawPart a = NewPart(new Vector3(-1, 0, 0));
            DrawPart b = NewPart(new Vector3(1, 0, 0));
            var wp = NewPart(new Vector3(0, 0, 1));
            var edge = new DrawEdge(a, b) { Waypoint = wp.transform };

            Vector3 atA = edge.TangentAt(0f);
            Vector3 atMid = edge.TangentAt(0.5f);
            Vector3 atB = edge.TangentAt(1f);

            Assert.AreEqual(1f, atA.magnitude, 1e-3f, "tangent should be normalised");
            Assert.Greater(atA.z, 0.5f, "leaving A the arc climbs +Z");
            Assert.Greater(atMid.x, 0.5f, "at the top the arc travels +X");
            Assert.Less(atB.z, -0.5f, "arriving at B the arc drops -Z");
        }
    }
}
