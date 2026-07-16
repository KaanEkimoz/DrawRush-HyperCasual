using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Authoring component for one paintable edge — the reusable "Kenar" prefab. It owns the
    /// two <see cref="DrawPart"/> spheres at its endpoints (children of the prefab — no corner
    /// sharing between edges), an optional waypoint that turns the edge into a circular arc, and
    /// the wall that is built + revealed once this edge is painted. The wall is generated
    /// procedurally along the edge geometry (see <see cref="ProceduralWall"/>) — no hand-built
    /// cube strips — so it fits any shape/length, straight or curved.
    /// <see cref="EdgeNetwork"/> reads these in the active level to build the runtime edges.
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

        [Header("Wall (procedural)")]
        [Tooltip("Optional wall material. If empty, a URP Lit material is generated.")]
        [SerializeField] private Material wallMaterial;
        [Tooltip("Wall (and drop) color for this edge.")]
        [SerializeField] private Color wallColor = new Color(0.85f, 0.2f, 0.18f);
        [SerializeField] private float wallHeight = 0.9f;
        [SerializeField] private float wallThickness = 0.7f;

        [Header("Drop color")]
        [Tooltip("When off, both drops take this edge's wall color. When on, use dropColor below.")]
        [SerializeField] private bool overrideDropColor;
        [SerializeField] private Color dropColor = new Color(0.10f, 0.85f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock _mpb;

        private DrawEdgeView _view;
        private ProceduralWall _wall;

        public DrawPart AnchorA => anchorA;
        public DrawPart AnchorB => anchorB;
        public Transform Waypoint => waypoint;
        public DrawEdgeView View => _view != null ? _view : (_view = GetComponent<DrawEdgeView>());
        public ProceduralWall Wall => _wall != null ? _wall : (_wall = GetComponent<ProceduralWall>());

        public float WallHeight => wallHeight;
        public float WallThickness => wallThickness;
        public Material WallMaterialAsset => wallMaterial;

        public bool IsValid => anchorA != null && anchorB != null && anchorA != anchorB;

        // Hidden on every enable (not just Awake) so the wall resets when the level is
        // re-activated (restart / revisit) — Awake does not run again on re-enable. The anchor
        // spheres are re-shown for the same reason: Reveal() turned them off last round.
        private void OnEnable()
        {
            Wall?.HideImmediate();
            if (anchorA != null) anchorA.gameObject.SetActive(true);
            if (anchorB != null) anchorB.gameObject.SetActive(true);
            ApplyDropColor();
        }

#if UNITY_EDITOR
        // Live preview in the editor when tweaking the wall/drop color.
        private void OnValidate() => ApplyDropColor();
#endif

        /// <summary>Tints both endpoint drops to this edge's color (wall color by default, or
        /// dropColor when overridden), via a MaterialPropertyBlock. Both drops share one color.</summary>
        public void ApplyDropColor()
        {
            Color c = overrideDropColor ? dropColor : wallColor;
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
            _mpb.SetColor(EmissionId, c * 0.45f);
            r.SetPropertyBlock(_mpb);
        }

        /// <summary>Build + raise this edge's procedural wall along the edge geometry, clear the
        /// painted line, and hide the two endpoint drops. Called by EdgeNetwork when the edge
        /// fills.</summary>
        public void Reveal(DrawEdge edge, Vector3 interiorPoint, Vector3 endA, Vector3 endB)
        {
            if (Wall != null)
            {
                Wall.Build(edge, wallHeight, wallThickness, wallMaterial, wallColor, interiorPoint, endA, endB);
                Wall.Reveal();
            }
            View.Hide();
            if (anchorA != null) anchorA.gameObject.SetActive(false);
            if (anchorB != null) anchorB.gameObject.SetActive(false);
        }

        /// <summary>This edge's color (for the painted line / corner). Fallback kept for
        /// call-site compatibility.</summary>
        public Color WallColor(Color fallback) => wallColor;

        /// <summary>Material for a corner cap to match the wall. May be null (generated).</summary>
        public Material WallMaterial() => wallMaterial;
    }
}
