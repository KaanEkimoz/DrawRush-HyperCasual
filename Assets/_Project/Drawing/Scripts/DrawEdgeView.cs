using System.Collections.Generic;
using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Renders the painted spans of one <see cref="DrawEdge"/> as ground-aligned
    /// LineRenderers. With free drawing an edge can be painted in any number of separate
    /// stretches, so painted spans and the dashed ghost (whatever is still unpainted) each use
    /// a small pool of renderers. Refreshes only when the edge's fill changes (it subscribes to
    /// <see cref="DrawEdge.Changed"/>); the only per-frame work is the gap-highlight pulse.
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

        [Header("Gap highlight (late in a level)")]
        [Tooltip("Ghost opacity while the remaining gaps are highlighted.")]
        [SerializeField, Range(0.1f, 1f)] private float highlightAlpha = 0.95f;
        [Tooltip("Ghost width multiple while highlighted.")]
        [SerializeField] private float highlightWidthScale = 1.6f;
        [Tooltip("Pulses per second of the highlighted ghost.")]
        [SerializeField] private float highlightPulse = 2.5f;

        private DrawEdge _edge;
        private readonly List<LineRenderer> _spans = new();
        private readonly List<LineRenderer> _ghosts = new();
        private readonly List<Vector2> _gaps = new();
        private bool _hidden;
        private bool _highlight;
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
            SetHighlight(false);
            foreach (LineRenderer lr in _spans) lr.enabled = false;
            foreach (LineRenderer lr in _ghosts) lr.enabled = false;
        }

        /// <summary>Make the still-unpainted stretches stand out (pulse, wider, brighter).</summary>
        public void SetHighlight(bool on)
        {
            if (_highlight == on) return;
            _highlight = on;
            if (!on) ApplyGhostColor(ghostAlpha, 1f);
        }

        private void Update()
        {
            if (!_highlight || _hidden) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * highlightPulse * 2f * Mathf.PI);
            ApplyGhostColor(Mathf.Lerp(ghostAlpha, highlightAlpha, k), Mathf.Lerp(1f, highlightWidthScale, k));
        }

        private void ApplyColor()
        {
            foreach (LineRenderer lr in _spans) { lr.startColor = color; lr.endColor = color; }
            ApplyGhostColor(ghostAlpha, 1f);
        }

        // The ghost wears the edge's own colour, faded — so it says what this edge will BE, and so
        // it stays visible on any ground. A flat white ghost disappeared completely against the
        // pale sand of the tutorial; it only ever looked right over the green levels.
        //
        // Via a property block rather than the LineRenderer's vertex colours: the ghost material is
        // URP/Unlit, which ignores vertex colour, and per-edge tinting must not instance the one
        // shared material.
        private void ApplyGhostColor(float alpha, float widthScale)
        {
            _ghostBlock ??= new MaterialPropertyBlock();
            var c = new Color(color.r, color.g, color.b, alpha);
            float w = ResolveWidth() * Mathf.Max(0.05f, ghostWidthScale) * widthScale;
            foreach (LineRenderer lr in _ghosts)
            {
                lr.GetPropertyBlock(_ghostBlock);
                _ghostBlock.SetColor(GhostBaseColorId, c);
                _ghostBlock.SetColor(GhostColorId, c);
                lr.SetPropertyBlock(_ghostBlock);
                lr.startWidth = w;
                lr.endWidth = w;
            }
        }

        private void OnDestroy()
        {
            if (_edge != null) _edge.Changed -= Refresh;
        }

        private void EnsureRenderers()
        {
            if (_spans.Count == 0) _spans.Add(CreateSpan("Span0"));
            if (_ghosts.Count == 0) _ghosts.Add(CreateGhost());
        }

        private LineRenderer SpanAt(int i)
        {
            while (_spans.Count <= i) { var lr = CreateSpan("Span" + _spans.Count); lr.startColor = color; lr.endColor = color; _spans.Add(lr); }
            return _spans[i];
        }

        private LineRenderer GhostAt(int i)
        {
            while (_ghosts.Count <= i) _ghosts.Add(CreateGhost());
            return _ghosts[i];
        }

        // The dashed hint showing where this edge still has to be drawn. Deliberately NOT the whole
        // edge: it renders only the unpainted stretch, so paint eats into it from either end rather
        // than being layered on top of it — no z-fighting, no double line, and the dots always mean
        // "what's left" instead of "what exists".
        private LineRenderer CreateGhost()
        {
            var go = new GameObject("GhostSpan" + _ghosts.Count);
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
            if (_ghostBlock != null || color != default)
            {
                _ghostBlock ??= new MaterialPropertyBlock();
                var c = new Color(color.r, color.g, color.b, ghostAlpha);
                _ghostBlock.SetColor(GhostBaseColorId, c);
                _ghostBlock.SetColor(GhostColorId, c);
                lr.SetPropertyBlock(_ghostBlock);
            }
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
            IReadOnlyList<Vector2> spans = fill.IsComplete ? Full : fill.Spans;

            // Painted stretches.
            int n = 0;
            for (; n < spans.Count; n++) SetSpan(SpanAt(n), spans[n].x, spans[n].y);
            for (int i = n; i < _spans.Count; i++) _spans[i].enabled = false;

            // Ghost = the complement: whatever is still unpainted.
            _gaps.Clear();
            float cursor = 0f;
            foreach (Vector2 s in spans) { if (s.x - cursor > 0.001f) _gaps.Add(new Vector2(cursor, s.x)); cursor = Mathf.Max(cursor, s.y); }
            if (1f - cursor > 0.001f) _gaps.Add(new Vector2(cursor, 1f));
            for (int i = 0; i < _gaps.Count; i++) SetGhost(GhostAt(i), _gaps[i].x, _gaps[i].y);
            for (int i = _gaps.Count; i < _ghosts.Count; i++) _ghosts[i].enabled = false;
        }

        private static readonly Vector2[] Full = { new(0f, 1f) };

        private void SetGhost(LineRenderer ghost, float t0, float t1)
        {
            int segs = _edge.IsArc ? Mathf.Max(2, Mathf.CeilToInt(arcSegments * (t1 - t0))) : 1;
            ghost.positionCount = segs + 1;
            for (int i = 0; i <= segs; i++)
            {
                Vector3 p = _edge.PointAt(Mathf.Lerp(t0, t1, (float)i / segs));
                p.y = ghostY;
                ghost.SetPosition(i, p);
            }
            ghost.enabled = true;
            // Dash density is NOT set here: LineTextureMode.Tile already repeats the pattern by
            // world length, and the rate lives in GhostPath.mat's tiling. Driving it from here
            // would both double-apply the length and force a material instance per edge.
        }

        private void SetSpan(LineRenderer lr, float t0, float t1)
        {
            // Straight edge → a single 2-point segment. Arc → sample the curve so the painted
            // line actually follows the bow.
            int segs = _edge.IsArc ? Mathf.Max(2, Mathf.CeilToInt(arcSegments * (t1 - t0))) : 1;
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
