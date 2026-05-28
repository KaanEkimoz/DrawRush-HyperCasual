using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Authoring component for one paintable edge — the reusable "Kenar" prefab. It owns the
    /// two <see cref="DrawPart"/> spheres at its endpoints (children of the prefab — no corner
    /// sharing between edges) and the wall segment that is revealed once this edge is painted.
    /// <see cref="EdgeNetwork"/> reads these in the active level to build the runtime edges, so
    /// shapes are composed edge-by-edge from prefabs instead of being derived from positions.
    /// </summary>
    [RequireComponent(typeof(DrawEdgeView))]
    public sealed class DrawEdgeAuthor : MonoBehaviour
    {
        [Header("Endpoints (local sphere children)")]
        [SerializeField] private DrawPart anchorA;
        [SerializeField] private DrawPart anchorB;

        [Header("Wall")]
        [Tooltip("Wall piece revealed (Animator plays) when this edge is fully painted. " +
                 "Starts hidden.")]
        [SerializeField] private GameObject wallSegment;

        private DrawEdgeView _view;

        public DrawPart AnchorA => anchorA;
        public DrawPart AnchorB => anchorB;
        public DrawEdgeView View => _view != null ? _view : (_view = GetComponent<DrawEdgeView>());

        public bool IsValid => anchorA != null && anchorB != null && anchorA != anchorB;

        // Hidden on every enable (not just Awake) so a revealed wall resets when the level is
        // re-activated (restart / revisit) — Awake does not run again on re-enable. The anchor
        // spheres are re-shown for the same reason: Reveal() turned them off last round.
        private void OnEnable()
        {
            if (wallSegment != null) wallSegment.SetActive(false);
            if (anchorA != null) anchorA.gameObject.SetActive(true);
            if (anchorB != null) anchorB.gameObject.SetActive(true);
        }

        /// <summary>Show this edge's wall segment (its Animator plays the reveal), clear the
        /// painted line, and hide the two endpoint spheres — once the edge is painted they have
        /// no further use and walking into them shouldn't re-attach the rail. Called by
        /// EdgeNetwork the moment the edge fills.</summary>
        public void Reveal()
        {
            if (wallSegment != null) wallSegment.SetActive(true);
            View.Hide();
            if (anchorA != null) anchorA.gameObject.SetActive(false);
            if (anchorB != null) anchorB.gameObject.SetActive(false);
        }

        /// <summary>The wall's color, so the painted line can match it. Falls back to
        /// <paramref name="fallback"/> when the wall has no readable color.</summary>
        public Color WallColor(Color fallback)
        {
            if (wallSegment == null) return fallback;
            Renderer r = wallSegment.GetComponentInChildren<Renderer>(includeInactive: true);
            Material m = r != null ? r.sharedMaterial : null;
            if (m == null) return fallback;
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            return fallback;
        }
    }
}
