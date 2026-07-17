using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Renders the painted spans of one <see cref="DrawEdge"/> as ground-aligned
    /// LineRenderers. Because an edge can be painted inward from both ends — leaving a gap in
    /// the middle until they meet — two renderers are used: the low span [A, paintedLow] and
    /// the high span [paintedHigh, B]. Refreshes only when the edge's fill advances (it
    /// subscribes to <see cref="DrawEdge.Changed"/>), never per frame.
    /// </summary>
    public sealed class DrawEdgeView : MonoBehaviour
    {
        [SerializeField] private Material lineMaterial;
        [Tooltip("Line width. When <= 0, falls back to GameConfig.lineWidth.")]
        [SerializeField] private float width = 0f;
        [Tooltip("World Y height of the painted line (kept just above the ground).")]
        [SerializeField] private float lineY = 0.02f;
        [Tooltip("Segments used to draw a curved (arc) span; straight spans use 1.")]
        [SerializeField] private int arcSegments = 24;
        [SerializeField] private Color color = new Color(0.1f, 1f, 0.8f, 1f);

        [Header("Ghost path (what is still to be drawn)")]
        [Tooltip("Dashed, faint material for the not-yet-painted stretch. Assign the real asset — " +
                 "the Shader.Find fallback only works in the editor.")]
        [SerializeField] private Material ghostMaterial;
        [Tooltip("Ghost width as a fraction of the painted line. Thinner reads as a hint rather " +
                 "than as a second, competing line.")]
        [SerializeField] private float ghostWidthScale = 0.55f;
        [Tooltip("Ghost opacity. It wears the edge's own colour at this alpha — a plain white ghost " +
                 "vanished entirely on the pale grounds (the tutorial's is nearly white).")]
        [SerializeField, Range(0.1f, 1f)] private float ghostAlpha = 0.4f;
        [Tooltip("Ghost Y. Just under the painted line so the two never z-fight at the boundary. " +
                 "Dash density lives in GhostPath.mat's tiling, not here — LineTextureMode.Tile " +
                 "already repeats by world length.")]
        [SerializeField] private float ghostY = 0.012f;

        private DrawEdge _edge;
        private LineRenderer _lowSpan;
        private LineRenderer _highSpan;
        private LineRenderer _ghostSpan;
        private bool _hidden;
        private MaterialPropertyBlock _ghostBlock;
        private static readonly int GhostBaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int GhostColorId = Shader.PropertyToID("_Color");

        /// <summary>Wire this view to an edge with the given line color (EdgeNetwork passes the
        /// edge's wall color so the painted line matches the wall).</summary>
        public void Bind(DrawEdge edge, Color lineColor)
        {
            if (_edge != null) _edge.Changed -= Refresh;
            _edge = edge;
            _hidden = false;
            color = lineColor;

            EnsureRenderers();
            ApplyColor();
            if (_edge != null) _edge.Changed += Refresh;
            Refresh();
        }

        /// <summary>Clear the painted line — called when this edge's wall is revealed, since the
        /// wall animation now shows the finished shape and the line would just be clutter.</summary>
        public void Hide()
        {
            _hidden = true;
            if (_lowSpan != null) _lowSpan.enabled = false;
            if (_highSpan != null) _highSpan.enabled = false;
            if (_ghostSpan != null) _ghostSpan.enabled = false;
        }

        private void ApplyColor()
        {
            if (_lowSpan != null) { _lowSpan.startColor = color; _lowSpan.endColor = color; }
            if (_highSpan != null) { _highSpan.startColor = color; _highSpan.endColor = color; }
            ApplyGhostColor();
        }

        // The ghost wears the edge's own colour, faded — so it says what this edge will BE, and so
        // it stays visible on any ground. A flat white ghost disappeared completely against the
        // pale sand of the tutorial; it only ever looked right over the green levels.
        //
        // Via a property block rather than the LineRenderer's vertex colours: the ghost material is
        // URP/Unlit, which ignores vertex colour, and per-edge tinting must not instance the one
        // shared material.
        private void ApplyGhostColor()
        {
            if (_ghostSpan == null) return;
            _ghostBlock ??= new MaterialPropertyBlock();
            _ghostSpan.GetPropertyBlock(_ghostBlock);
            var c = new Color(color.r, color.g, color.b, ghostAlpha);
            _ghostBlock.SetColor(GhostBaseColorId, c);
            _ghostBlock.SetColor(GhostColorId, c);
            _ghostSpan.SetPropertyBlock(_ghostBlock);
        }

        private void OnDestroy()
        {
            if (_edge != null) _edge.Changed -= Refresh;
        }

        private void EnsureRenderers()
        {
            if (_lowSpan == null) _lowSpan = CreateSpan("LowSpan");
            if (_highSpan == null) _highSpan = CreateSpan("HighSpan");
            if (_ghostSpan == null) _ghostSpan = CreateGhost();
        }

        // The dashed hint showing where this edge still has to be drawn. Deliberately NOT the whole
        // edge: it renders only the unpainted stretch, so paint eats into it from either end rather
        // than being layered on top of it — no z-fighting, no double line, and the dots always mean
        // "what's left" instead of "what exists".
        private LineRenderer CreateGhost()
        {
            var go = new GameObject("GhostSpan");
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.numCapVertices = 0;   // caps would fill in the gaps between dashes
            float w = ResolveWidth() * Mathf.Max(0.05f, ghostWidthScale);
            lr.startWidth = w;
            lr.endWidth = w;
            // Tile, not Stretch: the dash pattern has to repeat at a fixed rate along the line, or a
            // long edge would show one enormous dash and a short one a sliver of it.
            lr.textureMode = LineTextureMode.Tile;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.material = ResolveGhostMaterial();
            lr.enabled = false;
            return lr;
        }

        private Material ResolveGhostMaterial()
        {
            if (ghostMaterial != null) return ghostMaterial;
            // Editor-only net. In a build URP/Unlit is stripped unless referenced, so this returns
            // null and the ghost renders magenta — the exact failure the walls once shipped with.
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null)
            {
                Debug.LogError("DrawEdgeView has no ghostMaterial and URP/Unlit was not found — " +
                               "assign GhostPath.mat in the Inspector.", this);
                return null;
            }
            return new Material(s) { name = "Auto_GhostPath" };
        }

        private LineRenderer CreateSpan(string spanName)
        {
            var go = new GameObject(spanName);
            go.transform.SetParent(transform, false);

            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.numCapVertices = 2;

            float w = ResolveWidth();
            lr.startWidth = w;
            lr.endWidth = w;

            if (lineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) lineMaterial = new Material(shader) { name = "Auto_EdgeFillMat" };
            }
            lr.material = lineMaterial;
            lr.startColor = color;
            lr.endColor = color;
            lr.enabled = false;
            return lr;
        }

        private void Refresh()
        {
            if (_edge == null || _hidden) return;
            EdgeFill fill = _edge.Fill;

            if (fill.IsComplete)
            {
                SetSpan(_lowSpan, 0f, 1f);
                _highSpan.enabled = false;
                _ghostSpan.enabled = false;   // nothing left to hint at
                return;
            }

            if (fill.PaintedLow > 0.0001f) SetSpan(_lowSpan, 0f, fill.PaintedLow);
            else _lowSpan.enabled = false;

            if (fill.PaintedHigh < 0.9999f) SetSpan(_highSpan, fill.PaintedHigh, 1f);
            else _highSpan.enabled = false;

            // Whatever the two painted ends have not claimed yet.
            SetGhost(fill.PaintedLow, fill.PaintedHigh);
        }

        private void SetGhost(float t0, float t1)
        {
            if (t1 - t0 < 0.001f) { _ghostSpan.enabled = false; return; }

            int segs = _edge.IsArc ? Mathf.Max(2, arcSegments) : 1;
            _ghostSpan.positionCount = segs + 1;
            for (int i = 0; i <= segs; i++)
            {
                Vector3 p = _edge.PointAt(Mathf.Lerp(t0, t1, (float)i / segs));
                p.y = ghostY;
                _ghostSpan.SetPosition(i, p);
            }
            _ghostSpan.enabled = true;
            // Dash density is NOT set here: LineTextureMode.Tile already repeats the pattern by
            // world length, and the rate lives in GhostPath.mat's tiling. Driving it from here
            // would both double-apply the length and force a material instance per edge.
        }

        private void SetSpan(LineRenderer lr, float t0, float t1)
        {
            // Straight edge → a single 2-point segment. Arc → sample the curve so the painted
            // line actually follows the bow.
            int segs = _edge.IsArc ? Mathf.Max(2, arcSegments) : 1;
            lr.positionCount = segs + 1;
            for (int i = 0; i <= segs; i++)
            {
                Vector3 p = _edge.PointAt(Mathf.Lerp(t0, t1, (float)i / segs));
                p.y = lineY;
                lr.SetPosition(i, p);
            }
            lr.enabled = true;
        }

        private float ResolveWidth()
            => width > 0f ? width : (GameServices.Config != null ? GameServices.Config.lineWidth : 0.4f);
    }
}
