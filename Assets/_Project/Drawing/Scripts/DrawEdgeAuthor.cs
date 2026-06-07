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

        [Tooltip("Optional. Drop a Transform where the edge should bow; the edge becomes a " +
                 "circular arc through A → this point → B. Leave empty for a straight edge.")]
        [SerializeField] private Transform waypoint;

        [Header("Wall")]
        [Tooltip("Wall piece revealed (Animator plays) when this edge is fully painted. " +
                 "Starts hidden.")]
        [SerializeField] private GameObject wallSegment;

        [Header("Drop color")]
        [Tooltip("When off, both drops take this edge's wall color. When on, use dropColor " +
                 "below instead (per-edge override).")]
        [SerializeField] private bool overrideDropColor;
        [SerializeField] private Color dropColor = new Color(0.10f, 0.85f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;

        private DrawEdgeView _view;

        public DrawPart AnchorA => anchorA;
        public DrawPart AnchorB => anchorB;
        public Transform Waypoint => waypoint;
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
            ApplyDropColor();
        }

#if UNITY_EDITOR
        // Live preview in the editor when tweaking the wall color or the override.
        private void OnValidate() => ApplyDropColor();
#endif

        /// <summary>Tints both endpoint drops to this edge's color: the wall color by default,
        /// or <see cref="dropColor"/> when <see cref="overrideDropColor"/> is on. Applied via a
        /// MaterialPropertyBlock so the shared drop material isn't mutated. Both drops always
        /// share one color (one edge = one color).</summary>
        public void ApplyDropColor()
        {
            Color c = overrideDropColor ? dropColor : WallColor(dropColor);
            TintDrop(anchorA, c);
            TintDrop(anchorB, c);
        }

        private void TintDrop(DrawPart anchor, Color c)
        {
            if (anchor == null) return;
            var r = anchor.GetComponent<Renderer>();
            if (r == null) r = anchor.GetComponentInChildren<Renderer>(includeInactive: true);
            if (r == null) return;
            _mpb ??= new MaterialPropertyBlock();
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId, c);
            _mpb.SetColor(EmissionId, c * 0.45f);   // self-glow in its own color → pops off the ground
            r.SetPropertyBlock(_mpb);
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

        /// <summary>The wall's material, so a corner filler can match the wall's look.
        /// Null when there is no wall renderer.</summary>
        public Material WallMaterial()
        {
            if (wallSegment == null) return null;
            Renderer r = wallSegment.GetComponentInChildren<Renderer>(includeInactive: true);
            return r != null ? r.sharedMaterial : null;
        }

        /// <summary>World bounds of the wall segment, so a corner filler can match its height
        /// and base. Temporarily activates the (hidden) wall to read renderer bounds, then
        /// restores it. Returns false when there's no wall.</summary>
        public bool TryGetWallBounds(out Bounds bounds)
        {
            bounds = default;
            if (wallSegment == null) return false;
            bool wasActive = wallSegment.activeSelf;
            if (!wasActive) wallSegment.SetActive(true);
            var rends = wallSegment.GetComponentsInChildren<Renderer>(includeInactive: true);
            bool any = false;
            for (int i = 0; i < rends.Length; i++)
            {
                if (rends[i] == null) continue;
                if (!any) { bounds = rends[i].bounds; any = true; }
                else bounds.Encapsulate(rends[i].bounds);
            }
            if (!wasActive) wallSegment.SetActive(false);
            return any;
        }
    }
}
