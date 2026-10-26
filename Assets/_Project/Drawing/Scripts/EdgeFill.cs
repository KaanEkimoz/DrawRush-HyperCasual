using System.Collections.Generic;
using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Pure paint-progress state for one edge, on the edge's A (t=0) → B (t=1) axis. Paint is a
    /// set of painted spans that can be laid anywhere along the edge (free drawing), so a player
    /// can leave and rejoin mid-edge; touching spans merge. The edge is <see cref="IsComplete"/>
    /// once the spans cover [0, 1].
    ///
    /// Fresh paint can be laid as PENDING: it counts and shows, but an enemy hit can still wipe
    /// it (<see cref="ClearPending"/>) until it is confirmed a moment later
    /// (<see cref="ConfirmPendingOlderThan"/>). Confirmed paint is never lost.
    ///
    /// <see cref="PaintFromA"/> / <see cref="PaintFromB"/> keep the old ends-inward model working:
    /// they paint [0, t] and [t, 1], and <see cref="PaintedLow"/> / <see cref="PaintedHigh"/>
    /// report the spans that touch each end.
    ///
    /// No Unity dependencies beyond Mathf, so it is fully EditMode-testable.
    /// </summary>
    public sealed class EdgeFill
    {
        private const float Epsilon = 1e-4f;

        private readonly List<Vector2> _confirmed = new();          // merged, sorted spans (x = from, y = to)
        private readonly List<PendingSpan> _pending = new();
        private readonly List<Vector2> _union = new();
        private bool _unionDirty;

        private struct PendingSpan { public Vector2 Span; public float Time; }

        /// <summary>End of the painted span that touches end A (0 when none does).</summary>
        public float PaintedLow
        {
            get { var u = Union(); return u.Count > 0 && u[0].x <= Epsilon ? u[0].y : 0f; }
        }

        /// <summary>Start of the painted span that touches end B (1 when none does).</summary>
        public float PaintedHigh
        {
            get { var u = Union(); return u.Count > 0 && u[u.Count - 1].y >= 1f - Epsilon ? u[u.Count - 1].x : 1f; }
        }

        /// <summary>True once the painted spans cover the whole edge.</summary>
        public bool IsComplete { get { var u = Union(); return u.Count == 1 && u[0].x <= Epsilon && u[0].y >= 1f - Epsilon; } }

        /// <summary>Painted fraction of the edge in [0, 1] (pending paint included).</summary>
        public float Coverage
        {
            get
            {
                if (IsComplete) return 1f;
                float sum = 0f;
                foreach (Vector2 s in Union()) sum += s.y - s.x;
                return Mathf.Clamp01(sum);
            }
        }

        /// <summary>Every painted span, merged and sorted (pending included).</summary>
        public IReadOnlyList<Vector2> Spans => Union();

        /// <summary>True while some paint can still be wiped by a hit.</summary>
        public bool HasPending => _pending.Count > 0;

        /// <summary>Extend the painted span inward from end A up to <paramref name="t"/>.</summary>
        public void PaintFromA(float t) => PaintRange(0f, t);

        /// <summary>Extend the painted span inward from end B down to <paramref name="t"/>.</summary>
        public void PaintFromB(float t) => PaintRange(t, 1f);

        /// <summary>Paint [<paramref name="t0"/>, <paramref name="t1"/>] (either order) as confirmed.</summary>
        public void PaintRange(float t0, float t1) => AddConfirmed(Clamp(t0, t1));

        /// <summary>Paint a span as pending, stamped with <paramref name="time"/>.</summary>
        public void PaintPending(float t0, float t1, float time)
        {
            Vector2 s = Clamp(t0, t1);
            if (s.y - s.x <= 0f && s.x > 0f && s.y < 1f) s = new Vector2(Mathf.Max(0f, s.x - Epsilon), Mathf.Min(1f, s.y + Epsilon));
            _pending.Add(new PendingSpan { Span = s, Time = time });
            _unionDirty = true;
        }

        /// <summary>Confirm pending paint laid at or before <paramref name="time"/>.</summary>
        public void ConfirmPendingOlderThan(float time)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Time > time) continue;
                AddConfirmed(_pending[i].Span);
                _pending.RemoveAt(i);
            }
        }

        /// <summary>Confirm all pending paint.</summary>
        public void ConfirmAll() => ConfirmPendingOlderThan(float.PositiveInfinity);

        /// <summary>Wipe the not-yet-confirmed paint inside [<paramref name="t0"/>, <paramref name="t1"/>]
        /// only (an Eater walking over it); pending stretches outside survive. True if any was lost.</summary>
        public bool ErasePending(float t0, float t1)
        {
            Vector2 cut = Clamp(t0, t1);
            bool lost = false;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingSpan p = _pending[i];
                if (p.Span.y < cut.x || p.Span.x > cut.y) continue;
                _pending.RemoveAt(i);
                if (p.Span.x < cut.x) _pending.Add(new PendingSpan { Span = new Vector2(p.Span.x, cut.x), Time = p.Time });
                if (p.Span.y > cut.y) _pending.Add(new PendingSpan { Span = new Vector2(cut.y, p.Span.y), Time = p.Time });
                lost = true;
            }
            if (lost) _unionDirty = true;
            return lost;
        }

        /// <summary>Wipe paint that is not confirmed yet (an enemy hit). Returns true if any was lost.</summary>
        public bool ClearPending()
        {
            if (_pending.Count == 0) return false;
            _pending.Clear();
            _unionDirty = true;
            return true;
        }

        /// <summary>Close unpainted gaps no longer than <paramref name="maxGap"/> (in t units) that
        /// sit between two painted spans, and slivers at either edge end no longer than
        /// <paramref name="maxEndGap"/> (defaults to <paramref name="maxGap"/>).</summary>
        public bool CloseGaps(float maxGap, float maxEndGap = -1f)
        {
            ConfirmAll();
            if (maxEndGap < 0f) maxEndGap = maxGap;
            if (_confirmed.Count == 0 || (maxGap <= 0f && maxEndGap <= 0f)) return false;
            bool changed = false;
            if (_confirmed[0].x > Epsilon && _confirmed[0].x <= maxEndGap) { _confirmed[0] = new Vector2(0f, _confirmed[0].y); changed = true; }
            int last = _confirmed.Count - 1;
            if (_confirmed[last].y < 1f - Epsilon && 1f - _confirmed[last].y <= maxEndGap) { _confirmed[last] = new Vector2(_confirmed[last].x, 1f); changed = true; }
            for (int i = _confirmed.Count - 2; i >= 0; i--)
            {
                if (_confirmed[i + 1].x - _confirmed[i].y > maxGap) continue;
                _confirmed[i] = new Vector2(_confirmed[i].x, Mathf.Max(_confirmed[i].y, _confirmed[i + 1].y));
                _confirmed.RemoveAt(i + 1);
                changed = true;
            }
            if (changed) _unionDirty = true;
            return changed;
        }

        /// <summary>Painted fraction (pending included) if gaps up to <paramref name="maxGap"/> and end
        /// slivers up to <paramref name="maxEndGap"/> (t units) counted as painted, i.e. what
        /// <see cref="CloseGaps"/> would leave. Read-only.</summary>
        public float CoverageClosingGaps(float maxGap, float maxEndGap)
        {
            if (IsComplete) return 1f;
            IReadOnlyList<Vector2> u = Union();
            if (u.Count == 0) return 0f;
            float covered = 0f;
            float start = u[0].x <= maxEndGap ? 0f : u[0].x, end = u[0].y;
            for (int i = 1; i < u.Count; i++)
            {
                if (u[i].x - end <= maxGap) { end = Mathf.Max(end, u[i].y); continue; }
                covered += end - start;
                start = u[i].x;
                end = u[i].y;
            }
            if (1f - end <= maxEndGap) end = 1f;
            return covered + (end - start);
        }

        /// <summary>Paint the whole edge (auto-complete / win sweep).</summary>
        public void CompleteAll()
        {
            _pending.Clear();
            _confirmed.Clear();
            _confirmed.Add(new Vector2(0f, 1f));
            _unionDirty = true;
        }

        /// <summary>Clear all paint progress (edge fully unpainted again).</summary>
        public void Reset()
        {
            _confirmed.Clear();
            _pending.Clear();
            _unionDirty = true;
        }

        private static Vector2 Clamp(float t0, float t1)
        {
            float a = Mathf.Clamp01(Mathf.Min(t0, t1)), b = Mathf.Clamp01(Mathf.Max(t0, t1));
            return new Vector2(a, b);
        }

        private void AddConfirmed(Vector2 s)
        {
            if (s.y - s.x <= 0f) return;   // a point paints nothing
            Insert(_confirmed, s);
            _unionDirty = true;
        }

        // Insert a span into a merged, sorted list; spans that touch or overlap are joined.
        private static void Insert(List<Vector2> spans, Vector2 s)
        {
            int i = 0;
            while (i < spans.Count && spans[i].y < s.x - Epsilon) i++;
            while (i < spans.Count && spans[i].x <= s.y + Epsilon)
            {
                s = new Vector2(Mathf.Min(s.x, spans[i].x), Mathf.Max(s.y, spans[i].y));
                spans.RemoveAt(i);
            }
            spans.Insert(i, s);
        }

        private List<Vector2> Union()
        {
            if (!_unionDirty) return _union;
            _union.Clear();
            _union.AddRange(_confirmed);
            foreach (PendingSpan p in _pending) Insert(_union, p.Span);
            _unionDirty = false;
            return _union;
        }
    }
}
