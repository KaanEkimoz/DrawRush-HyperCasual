using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Drawing
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
        [SerializeField] private Color color = new Color(0.1f, 1f, 0.8f, 1f);

        private DrawEdge _edge;
        private PartManager _part;
        private LineRenderer _lowSpan;
        private LineRenderer _highSpan;
        private bool _hidden;

        /// <summary>Wire this view to an edge and (optionally) a shared fill material.</summary>
        public void Bind(DrawEdge edge, Material material)
        {
            if (_edge != null) _edge.Changed -= Refresh;
            if (_part != null) _part.Revealed -= Hide;
            _edge = edge;
            _hidden = false;
            if (material != null) lineMaterial = material;

            // The part that owns this edge's anchors (the Part object): drives the line color
            // and, on wall reveal, clears the line.
            _part = edge != null && edge.A != null
                ? edge.A.GetComponentInParent<PartManager>()
                : null;
            if (_part != null) color = _part.GetFillColor();

            EnsureRenderers();
            ApplyColor();
            if (_edge != null) _edge.Changed += Refresh;
            if (_part != null) _part.Revealed += Hide;
            Refresh();
        }

        // Clear the painted line once the part's wall is revealed — the wall animation now
        // shows the finished shape, so the trail would just be clutter.
        private void Hide()
        {
            _hidden = true;
            if (_lowSpan != null) _lowSpan.enabled = false;
            if (_highSpan != null) _highSpan.enabled = false;
        }

        private void ApplyColor()
        {
            if (_lowSpan != null) { _lowSpan.startColor = color; _lowSpan.endColor = color; }
            if (_highSpan != null) { _highSpan.startColor = color; _highSpan.endColor = color; }
        }

        private void OnDestroy()
        {
            if (_edge != null) _edge.Changed -= Refresh;
            if (_part != null) _part.Revealed -= Hide;
        }

        private void EnsureRenderers()
        {
            if (_lowSpan == null) _lowSpan = CreateSpan("LowSpan");
            if (_highSpan == null) _highSpan = CreateSpan("HighSpan");
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
                return;
            }

            if (fill.PaintedLow > 0.0001f) SetSpan(_lowSpan, 0f, fill.PaintedLow);
            else _lowSpan.enabled = false;

            if (fill.PaintedHigh < 0.9999f) SetSpan(_highSpan, fill.PaintedHigh, 1f);
            else _highSpan.enabled = false;
        }

        private void SetSpan(LineRenderer lr, float t0, float t1)
        {
            Vector3 p0 = _edge.PointAt(t0);
            Vector3 p1 = _edge.PointAt(t1);
            p0.y = lineY;
            p1.y = lineY;
            lr.SetPosition(0, p0);
            lr.SetPosition(1, p1);
            lr.enabled = true;
        }

        private float ResolveWidth()
            => width > 0f ? width : (GameServices.Config != null ? GameServices.Config.lineWidth : 0.4f);
    }
}
