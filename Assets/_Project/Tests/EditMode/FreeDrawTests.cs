using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using DrawRush.Drawing;

namespace DrawRush.Tests.EditMode
{
    /// <summary>Free drawing: painting anywhere along an edge, pending paint that a hit can wipe,
    /// gap closing, and the nearest-point query the painter snaps with.</summary>
    public sealed class FreeDrawTests
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
        public void PaintRange_InTheMiddle_LeavesBothEndsOpen()
        {
            var fill = new EdgeFill();
            fill.PaintRange(0.3f, 0.6f);
            Assert.AreEqual(0.3f, fill.Coverage, 1e-4f);
            Assert.AreEqual(0f, fill.PaintedLow);
            Assert.AreEqual(1f, fill.PaintedHigh);
            Assert.AreEqual(1, fill.Spans.Count);
        }

        [Test]
        public void OverlappingSpans_Merge_AndCoverTheEdge()
        {
            var fill = new EdgeFill();
            fill.PaintRange(0.6f, 1f);
            fill.PaintRange(0f, 0.2f);
            fill.PaintRange(0.15f, 0.65f);
            Assert.AreEqual(1, fill.Spans.Count);
            Assert.IsTrue(fill.IsComplete);
        }

        [Test]
        public void Pending_CountsButAHitWipesIt_ConfirmedStays()
        {
            var fill = new EdgeFill();
            fill.PaintPending(0f, 0.4f, time: 1f);
            fill.PaintPending(0.4f, 0.7f, time: 3f);
            Assert.AreEqual(0.7f, fill.Coverage, 1e-4f);

            fill.ConfirmPendingOlderThan(2f);   // only the first stretch is old enough
            Assert.IsTrue(fill.ClearPending());
            Assert.AreEqual(0.4f, fill.Coverage, 1e-4f);
            Assert.IsFalse(fill.ClearPending());
        }

        [Test]
        public void ErasePending_CutsOnlyTheEatenStretch()
        {
            var fill = new EdgeFill();
            fill.PaintRange(0f, 0.2f);                 // kept: an Eater cannot touch it
            fill.PaintPending(0.2f, 0.8f, time: 0f);
            Assert.IsTrue(fill.ErasePending(0.4f, 0.5f));
            Assert.AreEqual(0.7f, fill.Coverage, 1e-4f);
            Assert.AreEqual(2, fill.Spans.Count);        // [0, 0.4] and [0.5, 0.8]
            Assert.IsFalse(fill.ErasePending(0f, 0.15f), "kept paint is never eaten");
        }

        [Test]
        public void CloseGaps_EndSliversUseTheirOwnLimit()
        {
            var fill = new EdgeFill();
            fill.PaintRange(0.1f, 0.9f);
            Assert.IsFalse(fill.CloseGaps(0.05f, 0.05f));
            Assert.IsTrue(fill.CloseGaps(0.05f, 0.12f));
            Assert.IsTrue(fill.IsComplete);
        }

        [Test]
        public void CloseGaps_FillsShortGapsOnly()
        {
            var fill = new EdgeFill();
            fill.PaintRange(0.02f, 0.5f);   // 0.02 gap at A
            fill.PaintRange(0.53f, 0.8f);   // 0.03 gap in the middle, 0.2 gap at B
            Assert.IsTrue(fill.CloseGaps(0.05f));
            Assert.AreEqual(1, fill.Spans.Count);
            Assert.AreEqual(0f, fill.Spans[0].x, 1e-4f);
            Assert.AreEqual(0.8f, fill.Spans[0].y, 1e-4f);
            Assert.IsFalse(fill.IsComplete);
        }

        [Test]
        public void EdgePaintPending_CompletingConfirmsAndFiresOnce()
        {
            var edge = new DrawEdge(NewPart(Vector3.zero), NewPart(new Vector3(4, 0, 0)));
            int fired = 0;
            edge.Completed += _ => fired++;
            edge.PaintPending(0f, 0.5f, 0f);
            edge.PaintPending(0.5f, 1f, 0f);
            Assert.IsTrue(edge.IsComplete);
            Assert.AreEqual(1, fired);
            Assert.IsFalse(edge.ClearPending(), "completion keeps the paint for good");
        }

        [Test]
        public void ClosestT_OnAStraightEdge_ProjectsAndMeasures()
        {
            var edge = new DrawEdge(NewPart(Vector3.zero), NewPart(new Vector3(4, 0, 0)));
            float t = edge.ClosestT(new Vector3(1f, 5f, 0.3f), out float d);
            Assert.AreEqual(0.25f, t, 1e-4f);
            Assert.AreEqual(0.3f, d, 1e-4f);
            Assert.AreEqual(1f, edge.ClosestT(new Vector3(9f, 0f, 0f), out _), 1e-4f);
        }

        [Test]
        public void ClosestT_OnAnArc_FollowsTheCurve()
        {
            var waypoint = new GameObject("Waypoint");
            _spawned.Add(waypoint);
            waypoint.transform.position = new Vector3(0f, 0f, 2f);   // half circle of radius 2 through (0,0,2)
            var edge = new DrawEdge(NewPart(new Vector3(-2f, 0f, 0f)), NewPart(new Vector3(2f, 0f, 0f))) { Waypoint = waypoint.transform };
            float t = edge.ClosestT(new Vector3(0f, 0f, 2.2f), out float d);
            Assert.AreEqual(0.5f, t, 1e-3f);
            Assert.AreEqual(0.2f, d, 1e-3f);
        }
    }
}
