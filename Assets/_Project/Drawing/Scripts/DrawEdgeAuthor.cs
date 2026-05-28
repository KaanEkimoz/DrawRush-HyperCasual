using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Authoring component for one paintable edge — the reusable "Kenar" prefab. It references
    /// the two corner anchors the edge spans (shared <see cref="DrawPart"/> spheres placed once
    /// per corner) and carries the wall segment that is revealed once this edge is painted.
    /// <see cref="EdgeNetwork"/> reads these in the active level to build the runtime edges, so
    /// shapes are composed edge-by-edge from prefabs instead of being derived from positions.
    /// </summary>
    [RequireComponent(typeof(DrawEdgeView))]
    public sealed class DrawEdgeAuthor : MonoBehaviour
    {
        [Header("Endpoints (shared corner spheres)")]
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

        private void Awake()
        {
            if (wallSegment != null) wallSegment.SetActive(false);
        }

        /// <summary>Show this edge's wall segment (its Animator plays the reveal) and clear the
        /// painted line — called by EdgeNetwork the moment the edge fills.</summary>
        public void Reveal()
        {
            if (wallSegment != null) wallSegment.SetActive(true);
            View.Hide();
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
