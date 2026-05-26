using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Pure paint-progress state for one edge. The edge runs from end A (t=0) to end B
    /// (t=1) and can be painted inward from either end: <see cref="PaintedLow"/> grows from
    /// 0 toward 1 as the A side is painted, <see cref="PaintedHigh"/> shrinks from 1 toward
    /// 0 as the B side is painted. The edge is <see cref="IsComplete"/> once the two painted
    /// spans meet. Partial paint only ever advances — never lost — so an enemy interrupting
    /// the player mid-edge leaves the already-painted portion intact.
    ///
    /// No Unity dependencies beyond Mathf, so it is fully EditMode-testable.
    /// </summary>
    public sealed class EdgeFill
    {
        /// <summary>Painted span from end A is [0, PaintedLow]. Only grows.</summary>
        public float PaintedLow { get; private set; }

        /// <summary>Painted span from end B is [PaintedHigh, 1]. Only shrinks.</summary>
        public float PaintedHigh { get; private set; } = 1f;

        /// <summary>True once the two painted spans meet (whole edge covered).</summary>
        public bool IsComplete => PaintedLow >= PaintedHigh;

        /// <summary>Total painted fraction of the edge in [0, 1].</summary>
        public float Coverage => IsComplete ? 1f : Mathf.Clamp01(PaintedLow + (1f - PaintedHigh));

        /// <summary>Extend the painted span inward from end A up to <paramref name="t"/>.</summary>
        public void PaintFromA(float t) => PaintedLow = Mathf.Max(PaintedLow, Mathf.Clamp01(t));

        /// <summary>Extend the painted span inward from end B down to <paramref name="t"/>.</summary>
        public void PaintFromB(float t) => PaintedHigh = Mathf.Min(PaintedHigh, Mathf.Clamp01(t));

        /// <summary>Clear all paint progress (edge fully unpainted again).</summary>
        public void Reset()
        {
            PaintedLow = 0f;
            PaintedHigh = 1f;
        }
    }
}
