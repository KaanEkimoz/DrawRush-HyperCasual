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
    /// Owns topology, fill state, and world-space geometry. Geometry is a straight A→B segment
    /// by default; assigning <see cref="Waypoint"/> turns it into a circular arc that passes
    /// through A, the waypoint, and B (the true 3-point circle). All geometry queries
    /// (<see cref="PointAt"/>, <see cref="TangentAt"/>, <see cref="Length"/>) honor this, so
    /// the rail movement, painted-line view, and procedural wall are all geometry-agnostic.
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

        /// <summary>Optional third point. When set (and not collinear with A/B), the edge is a
        /// circular arc through A → Waypoint → B. When null, the edge is a straight A→B line.</summary>
        public Transform Waypoint { get; set; }

        private bool _completedRaised;

        public DrawEdge(DrawPart a, DrawPart b)
        {
            A = a != null ? a : throw new ArgumentNullException(nameof(a));
            B = b != null ? b : throw new ArgumentNullException(nameof(b));
            Fill = new EdgeFill();
        }

        public bool IsComplete => Fill.IsComplete;

        /// <summary>True when this edge currently resolves to a circular arc (waypoint set and
        /// not collinear/degenerate).</summary>
        public bool IsArc => TryComputeArc(out _, out _, out _, out _, out _);

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
        public Vector3 PointAt(float t)
        {
            t = Mathf.Clamp01(t);
            if (!TryComputeArc(out Vector2 center, out float radius, out float angA, out float sweep, out float y))
                return Vector3.Lerp(A.Transform.position, B.Transform.position, t);

            // Pin the endpoints exactly to A/B (trig at t=0/1 drifts a hair off, and the rail /
            // corner alignment wants the ends precise).
            if (t <= 0f) return A.Transform.position;
            if (t >= 1f) return B.Transform.position;

            float ang = angA + sweep * t;
            return new Vector3(center.x + radius * Mathf.Cos(ang), y, center.y + radius * Mathf.Sin(ang));
        }

        /// <summary>Unit forward direction (A→B sense) at parameter <paramref name="t"/>.</summary>
        public Vector3 TangentAt(float t)
        {
            if (!TryComputeArc(out Vector2 center, out float radius, out float angA, out float sweep, out _))
            {
                Vector3 d = B.Transform.position - A.Transform.position;
                d.y = 0f;
                return d.sqrMagnitude > 1e-8f ? d.normalized : Vector3.forward;
            }
            float ang = angA + sweep * Mathf.Clamp01(t);
            // d/dt of (cos,sin) is (-sin,cos); flip with the sweep sign so it points A→B.
            float s = Mathf.Sign(sweep);
            var tan = new Vector3(-Mathf.Sin(ang) * s, 0f, Mathf.Cos(ang) * s);
            return tan.sqrMagnitude > 1e-8f ? tan.normalized : Vector3.forward;
        }

        /// <summary>World-space length of the edge (straight distance or arc length).</summary>
        public float Length
        {
            get
            {
                if (!TryComputeArc(out _, out float radius, out _, out float sweep, out _))
                    return Vector3.Distance(A.Transform.position, B.Transform.position);
                return Mathf.Abs(sweep) * radius;
            }
        }

        // Circle through A, Waypoint, B in the XZ plane. Returns false (→ treat as straight) when
        // there's no waypoint, the three points are (near) collinear, or the radius is implausibly
        // large. Out params describe the arc that starts at A (angA), sweeps `sweep` radians
        // (signed; chosen so the arc passes through the waypoint) and ends at B.
        private bool TryComputeArc(out Vector2 center, out float radius, out float angA, out float sweep, out float y)
        {
            center = default; radius = 0f; angA = 0f; sweep = 0f; y = 0f;
            if (Waypoint == null) return false;

            Vector3 pa = A.Transform.position, pb = B.Transform.position, pc = Waypoint.position;
            y = pa.y;
            Vector2 a = new(pa.x, pa.z), b = new(pb.x, pb.z), c = new(pc.x, pc.z);

            float d = 2f * (a.x * (b.y - c.y) + b.x * (c.y - a.y) + c.x * (a.y - b.y));
            if (Mathf.Abs(d) < 1e-5f) return false;   // collinear → straight

            float a2 = a.sqrMagnitude, b2 = b.sqrMagnitude, c2 = c.sqrMagnitude;
            float ux = (a2 * (b.y - c.y) + b2 * (c.y - a.y) + c2 * (a.y - b.y)) / d;
            float uy = (a2 * (c.x - b.x) + b2 * (a.x - c.x) + c2 * (b.x - a.x)) / d;
            center = new Vector2(ux, uy);
            radius = Vector2.Distance(center, a);
            if (radius > 1e4f) return false;          // practically straight

            angA = Mathf.Atan2(a.y - uy, a.x - ux);
            float angB = Mathf.Atan2(b.y - uy, b.x - ux);
            float angC = Mathf.Atan2(c.y - uy, c.x - ux);

            float ccwAB = Norm2Pi(angB - angA);       // CCW sweep A→B in [0,2π)
            float ccwAC = Norm2Pi(angC - angA);        // CCW position of waypoint
            // If the waypoint lies on the CCW arc A→B, sweep CCW (positive); else go CW (negative).
            sweep = ccwAC <= ccwAB ? ccwAB : ccwAB - 2f * Mathf.PI;
            return true;
        }

        private static float Norm2Pi(float a)
        {
            const float twoPi = 2f * Mathf.PI;
            a %= twoPi;
            return a < 0f ? a + twoPi : a;
        }
    }
}
