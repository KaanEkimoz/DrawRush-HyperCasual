using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// The one place that decides what lies on top of what on the page. Every flat thing drawn on
    /// the ground (arena line, pencil trail, shape fills, finished lines, the dotted ghost of what is
    /// still to draw) is a transparent renderer with ZWrite off, so neither its height nor its
    /// material queue decides the order: URP sorts them back to front by the distance of each
    /// renderer's bounds, and a long pencil trail kept for minutes ends up "nearer" than half the
    /// shape and paints over it. Renderer.sortingOrder outranks both the render queue and that
    /// distance sort, so each layer gets a fixed order here and the stack is the same from any
    /// camera angle.
    ///
    /// Top to bottom (Kaan): unfinished shape (ghost dots) over finished lines, over the fill
    /// colour, over the pencil trail, over the paper. All orders stay below -50 so ground hazards
    /// and effects at the default order (glue puddles, attack cones, shockwaves, particles) still
    /// draw above the drawing.
    ///
    /// The heights only keep the same stack against the opaque paper and desk props (the ZTest);
    /// they rise with the layer so nothing contradicts the order.
    /// </summary>
    public static class GroundLayers
    {
        /// <summary>Render queue every ground layer's own material uses (plain Transparent).
        /// Kept equal on purpose: the order comes from <see cref="Renderer.sortingOrder"/>.</summary>
        public const int Queue = 3000;

        public const int ArenaOrder = -1000;
        public const int GlowOrder = -900;          // soft disc under the brush while it paints
        public const int TrailOrder = -890;         // the pencil trail
        public const int FillOrder = -800;          // + 0..99, bigger loops lower (eye over face)
        public const int FillOrderRange = 99;
        public const int LineOrder = -600;          // painted / finished edge lines and corner joints
        public const int GhostShadowOrder = -510;   // dark track under the dashes
        public const int GhostOrder = -500;         // dotted outline of what is still to draw: top

        public const float ArenaY = 0.004f;
        public const float GlowY = 0.006f;
        public const float TrailY = 0.008f;
        public const float FillY = 0.010f;
        public const float LineY = 0.014f;
        public const float GhostShadowY = 0.016f;
        public const float GhostY = 0.018f;

        /// <summary>Put <paramref name="r"/> on the given layer.</summary>
        public static void Apply(Renderer r, int order)
        {
            if (r == null) return;
            r.sortingLayerID = 0;   // Default
            r.sortingOrder = order;
        }

        /// <summary>For a material this code created itself (never a shared asset).</summary>
        public static Material Owned(Material m)
        {
            if (m != null) m.renderQueue = Queue;
            return m;
        }
    }
}
