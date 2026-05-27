using System;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Runtime representation of one paintable edge between two neighboring anchors. Bundles
    /// the two <see cref="DrawPart"/> endpoints with a pure <see cref="EdgeFill"/> progress
    /// state and routes paint input to the correct end based on which anchor the player
    /// entered from. Fires <see cref="Completed"/> exactly once — the first time the two
    /// painted spans meet — so <see cref="EdgeNetwork"/> can count down remaining edges.
    ///
    /// Owns topology, fill state, and world-space geometry only. The painted-span visual
    /// (LineRenderer) is wired by the paint controller in a later step, where it is tuned
    /// together with ray movement.
    /// </summary>
    public sealed class DrawEdge
    {
        /// <summary>Raised once, the first time this edge becomes fully painted.</summary>
        public event Action<DrawEdge> Completed;

        /// <summary>Raised whenever the painted span advances, so a view can refresh
        /// without polling every frame.</summary>
        public event Action Changed;

        public DrawPart A { get; }
        public DrawPart B { get; }
        public EdgeFill Fill { get; }

        private bool _completedRaised;

        public DrawEdge(DrawPart a, DrawPart b)
        {
            A = a != null ? a : throw new ArgumentNullException(nameof(a));
            B = b != null ? b : throw new ArgumentNullException(nameof(b));
            Fill = new EdgeFill();
        }

        public bool IsComplete => Fill.IsComplete;

        /// <summary>True if <paramref name="part"/> is one of this edge's two endpoints.</summary>
        public bool Contains(DrawPart part) => part == A || part == B;

        /// <summary>The endpoint opposite <paramref name="part"/>, or null if not an endpoint.</summary>
        public DrawPart Other(DrawPart part)
        {
            if (part == A) return B;
            if (part == B) return A;
            return null;
        }

        /// <summary>
        /// Paint inward from <paramref name="fromEnd"/> up to parameter <paramref name="t"/>,
        /// measured along A (t=0) → B (t=1). Entering from A grows the low span; entering from
        /// B shrinks the high span. Both ends share the same t axis. Raises
        /// <see cref="Completed"/> the first time the edge fills. A no-op if
        /// <paramref name="fromEnd"/> is not an endpoint.
        /// </summary>
        public void PaintFrom(DrawPart fromEnd, float t)
        {
            float low = Fill.PaintedLow;
            float high = Fill.PaintedHigh;

            if (fromEnd == A) Fill.PaintFromA(t);
            else if (fromEnd == B) Fill.PaintFromB(t);
            else return;

            if (Fill.PaintedLow != low || Fill.PaintedHigh != high) Changed?.Invoke();

            if (!_completedRaised && Fill.IsComplete)
            {
                _completedRaised = true;
                Completed?.Invoke(this);
            }
        }

        /// <summary>World-space point along the edge at parameter <paramref name="t"/> (A→B).</summary>
        public Vector3 PointAt(float t) =>
            Vector3.Lerp(A.Transform.position, B.Transform.position, Mathf.Clamp01(t));
    }
}
