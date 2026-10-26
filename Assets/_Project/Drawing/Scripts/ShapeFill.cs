using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// The inside of a finished shape: finds the closed loops a level's edges make and builds a flat
    /// tinted mesh for each, so finishing a shape leaves a filled picture (a red watermelon, a
    /// flower's petals) instead of a bare outline. Geometry only; <see cref="EdgeNetwork"/> decides
    /// when a fill shows.
    /// </summary>
    public static class ShapeFill
    {
        public sealed class Loop
        {
            public readonly List<DrawEdge> Edges = new();
            public readonly List<Vector3> Points = new();   // ground outline, in walking order
        }

        /// <summary>Closed loops in the edge graph. <paramref name="cornerOf"/> maps an edge end to
        /// its corner id; an edge with both ends in one corner, or one <paramref name="isPetal"/>
        /// accepts (a curve whose ends nearly meet), is a loop by itself. Open chains (shapes that
        /// do not close) give no loop.</summary>
        public static List<Loop> FindLoops(IReadOnlyList<DrawEdge> edges, System.Func<DrawPart, int> cornerOf,
                                           System.Func<DrawEdge, bool> isPetal = null, int arcSegments = 16)
        {
            var loops = new List<Loop>();
            var used = new HashSet<DrawEdge>();

            bool Alone(DrawEdge e) => cornerOf(e.A) == cornerOf(e.B) || (isPetal != null && isPetal(e));

            foreach (DrawEdge e in edges)
            {
                if (!Alone(e)) continue;
                var loop = new Loop();
                loop.Edges.Add(e);
                Sample(e, true, loop.Points, arcSegments, skipFirst: false);
                used.Add(e);
                if (loop.Points.Count >= 3) loops.Add(loop);
            }

            foreach (DrawEdge start in edges)
            {
                if (used.Contains(start)) continue;
                var loop = new Loop();
                var chain = new HashSet<DrawEdge> { start };
                loop.Edges.Add(start);
                Sample(start, true, loop.Points, arcSegments, skipFirst: false);
                int home = cornerOf(start.A), at = cornerOf(start.B);
                bool closed = false;
                for (int guard = 0; guard < edges.Count; guard++)
                {
                    if (at == home) { closed = true; break; }
                    DrawEdge next = null;
                    bool forward = true;
                    foreach (DrawEdge c in edges)
                    {
                        if (used.Contains(c) || chain.Contains(c) || Alone(c)) continue;
                        if (cornerOf(c.A) == at) { next = c; forward = true; break; }
                        if (cornerOf(c.B) == at) { next = c; forward = false; break; }
                    }
                    if (next == null) break;
                    chain.Add(next);
                    loop.Edges.Add(next);
                    Sample(next, forward, loop.Points, arcSegments, skipFirst: true);
                    at = forward ? cornerOf(next.B) : cornerOf(next.A);
                }
                if (!closed || loop.Points.Count < 3) continue;
                foreach (DrawEdge c in chain) used.Add(c);
                loops.Add(loop);
            }
            return loops;
        }

        private static void Sample(DrawEdge e, bool forward, List<Vector3> into, int arcSegments, bool skipFirst)
        {
            int segs = e.IsArc ? arcSegments : 1;
            for (int i = skipFirst ? 1 : 0; i <= segs; i++)
            {
                float t = (float)i / segs;
                into.Add(e.PointAt(forward ? t : 1f - t));
            }
        }

        /// <summary>Flat mesh filling <paramref name="outline"/> (a simple polygon on the ground,
        /// either winding) at height <paramref name="y"/>, tinted <paramref name="color"/>.
        /// Null if it cannot be triangulated.</summary>
        public static Mesh BuildMesh(List<Vector3> outline, float y, Color color)
        {
            var pts = new List<Vector2>(outline.Count);
            foreach (Vector3 p in outline)
            {
                var q = new Vector2(p.x, p.z);
                if (pts.Count == 0 || (pts[pts.Count - 1] - q).sqrMagnitude > 1e-6f) pts.Add(q);
            }
            if (pts.Count > 2 && (pts[0] - pts[pts.Count - 1]).sqrMagnitude < 1e-6f) pts.RemoveAt(pts.Count - 1);
            if (pts.Count < 3) return null;
            if (SignedArea(pts) < 0f) pts.Reverse();   // counter-clockwise for the ear test

            List<int> tris = Triangulate(pts);
            if (tris == null || tris.Count < 3) return null;

            var verts = new Vector3[pts.Count];
            var cols = new Color[pts.Count];
            for (int i = 0; i < pts.Count; i++) { verts[i] = new Vector3(pts[i].x, y, pts[i].y); cols[i] = color; }
            // Facing up: the ear clipper emits counter-clockwise in XZ, which looks down from +Y.
            for (int i = 0; i < tris.Count; i += 3) (tris[i + 1], tris[i + 2]) = (tris[i + 2], tris[i + 1]);
            var mesh = new Mesh { name = "ShapeFill" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float SignedArea(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i++) a += (p[j].x * p[i].y) - (p[i].x * p[j].y);
            return a * 0.5f;
        }

        // Ear clipping for a counter-clockwise simple polygon. O(n^3) worst case; outlines here
        // stay under a few hundred points.
        private static List<int> Triangulate(List<Vector2> p)
        {
            var idx = new List<int>(p.Count);
            for (int i = 0; i < p.Count; i++) idx.Add(i);
            var tris = new List<int>((p.Count - 2) * 3);
            int guard = p.Count * p.Count;
            while (idx.Count > 3 && guard-- > 0)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int a = idx[(i + idx.Count - 1) % idx.Count], b = idx[i], c = idx[(i + 1) % idx.Count];
                    if (Cross(p[a], p[b], p[c]) <= 1e-7f) continue;   // reflex or flat corner
                    bool inside = false;
                    for (int k = 0; k < idx.Count && !inside; k++)
                    {
                        int q = idx[k];
                        if (q == a || q == b || q == c) continue;
                        inside = InTriangle(p[q], p[a], p[b], p[c]);
                    }
                    if (inside) continue;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    idx.RemoveAt(i);
                    clipped = true;
                    break;
                }
                if (!clipped) break;   // not a simple polygon: give up rather than draw garbage
            }
            if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            return idx.Count == 3 ? tris : null;
        }

        private static float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);

        private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
            => Cross(a, b, p) >= 0f && Cross(b, c, p) >= 0f && Cross(c, a, p) >= 0f;
    }
}
