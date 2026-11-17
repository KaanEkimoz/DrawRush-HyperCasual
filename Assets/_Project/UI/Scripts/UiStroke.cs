using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DrawRush.UI
{
    /// <summary>
    /// A thick polyline drawn as UI geometry, revealed along its length by <see cref="Progress"/>
    /// (0..1). Optional dashes turn it into the game's dotted guide line. Points are in this
    /// rect's local space.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UiStroke : MaskableGraphic
    {
        public List<Vector2> Points = new();
        public bool Closed = true;
        public float Width = 22f;
        [Range(0f, 1f)] public float Progress = 1f;
        [Tooltip("Dash and gap length (0 = solid).")]
        public float Dash, Gap;

        public void Redraw() => SetVerticesDirty();

        /// <summary>Point and direction at fraction <paramref name="f"/> of the length.</summary>
        public Vector2 PointAt(float f, out Vector2 dir)
        {
            dir = Vector2.right;
            int n = Points.Count;
            if (n < 2) return n == 1 ? Points[0] : Vector2.zero;
            float total = Length(), want = Mathf.Clamp01(f) * total, run = 0f;
            int segs = Closed ? n : n - 1;
            for (int i = 0; i < segs; i++)
            {
                Vector2 a = Points[i], b = Points[(i + 1) % n];
                float l = Vector2.Distance(a, b);
                if (run + l >= want || i == segs - 1)
                {
                    dir = l > 0f ? (b - a) / l : dir;
                    return Vector2.Lerp(a, b, l > 0f ? (want - run) / l : 0f);
                }
                run += l;
            }
            return Points[0];
        }

        public float Length()
        {
            float total = 0f;
            int n = Points.Count, segs = Closed ? n : n - 1;
            for (int i = 0; i < segs; i++) total += Vector2.Distance(Points[i], Points[(i + 1) % n]);
            return total;
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            int n = Points.Count;
            if (n < 2 || Progress <= 0f) return;
            float total = Length(), end = total * Progress, run = 0f;
            int segs = Closed ? n : n - 1;
            float half = Width * 0.5f;
            for (int i = 0; i < segs && run < end; i++)
            {
                Vector2 a = Points[i], b = Points[(i + 1) % n];
                float l = Vector2.Distance(a, b);
                if (l <= 0f) continue;
                Vector2 dir = (b - a) / l;
                float from = 0f, to = Mathf.Min(l, end - run);
                if (Dash > 0f)
                {
                    float period = Dash + Gap;
                    for (float s = -((run) % period); s < to; s += period)
                    {
                        float d0 = Mathf.Max(from, s), d1 = Mathf.Min(to, s + Dash);
                        if (d1 > d0) Quad(vh, a + dir * d0, a + dir * d1, dir, half);
                    }
                }
                else
                {
                    Quad(vh, a + dir * from, a + dir * to, dir, half);
                    Cap(vh, a + dir * to, half);   // rounds the joints and the moving tip
                }
                run += l;
            }
            if (Dash <= 0f) Cap(vh, Points[0], half);
        }

        private void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 dir, float half)
        {
            Vector2 nrm = new Vector2(-dir.y, dir.x) * half;
            int i = vh.currentVertCount;
            vh.AddVert(a - nrm, color, Vector2.zero);
            vh.AddVert(a + nrm, color, Vector2.zero);
            vh.AddVert(b + nrm, color, Vector2.zero);
            vh.AddVert(b - nrm, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }

        private void Cap(VertexHelper vh, Vector2 c, float r)
        {
            const int seg = 14;
            int centre = vh.currentVertCount;
            vh.AddVert(c, color, Vector2.zero);
            for (int k = 0; k <= seg; k++)
            {
                float ang = k * Mathf.PI * 2f / seg;
                vh.AddVert(c + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r, color, Vector2.zero);
                if (k > 0) vh.AddTriangle(centre, centre + k, centre + k + 1);
            }
        }
    }
}
