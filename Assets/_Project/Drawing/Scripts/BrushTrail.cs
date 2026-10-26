using DrawRush.Core;
using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// A thin paint trail behind the player that fades in a couple of seconds, wherever they walk.
    /// It is only feedback ("I am the brush"): coverage still comes from the snapped paint on the
    /// template, so the shape stays clean while every step still leaves a mark. From the redesign
    /// research: off-template paint is cosmetic and fades.
    /// </summary>
    [RequireComponent(typeof(FreeDrawPainter))]
    public sealed class BrushTrail : MonoBehaviour
    {
        [Tooltip("Used until a level's own paint colour is known.")]
        [SerializeField] private Color color = new(0.1f, 1f, 0.8f, 1f);
        [Tooltip("Opacity at the brush; the trail fades to clear at its tail.")]
        [SerializeField, Range(0f, 1f)] private float opacity = 0.75f;
        [Tooltip("Trail width (m) at the brush; it tapers to nothing at the tail.")]
        [SerializeField] private float width = 0.3f;
        [Tooltip("Seconds a stretch of trail takes to fade out.")]
        [SerializeField] private float fadeSeconds = 2.5f;
        [Tooltip("Height (m) above the player's feet, under the painted template line.")]
        [SerializeField] private float lift = 0.015f;
        [Tooltip("Ground speed (m/s) below which no new trail is laid.")]
        [SerializeField] private float minSpeed = 0.2f;

        private FreeDrawPainter _painter;
        private TrailRenderer _trail;
        private Vector3 _lastPos;

        private void Awake()
        {
            _painter = GetComponent<FreeDrawPainter>();
            var go = new GameObject("BrushTrail");
            // Not parented: the trail must stay flat on the ground whatever the player's rotation.
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            _trail = go.AddComponent<TrailRenderer>();
            _trail.alignment = LineAlignment.TransformZ;
            _trail.time = fadeSeconds;
            _trail.minVertexDistance = 0.1f;
            _trail.numCapVertices = 2;
            _trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            _trail.widthMultiplier = width;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.material = new Material(Shader.Find("Sprites/Default")) { name = "Auto_BrushTrail" };
            SetColor(color);
            _trail.emitting = false;
        }

        private void OnEnable() => _painter.LevelBound += OnLevelBound;
        private void OnDisable() => _painter.LevelBound -= OnLevelBound;

        private void OnDestroy()
        {
            if (_trail == null) return;
            Destroy(_trail.material);
            Destroy(_trail.gameObject);
        }

        // A new level (or a retry) teleports the player: never draw a streak across the map.
        private void OnLevelBound(EdgeNetwork network)
        {
            // Same colour as the line being painted, so the trail reads as that paint.
            if (network != null && network.Edges.Count > 0) SetColor(network.ColorOf(network.Edges[0]));
            _lastPos = transform.position;
            _trail.transform.position = _lastPos + Vector3.up * lift;
            _trail.Clear();
        }

        private void SetColor(Color c)
        {
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                      new[] { new GradientAlphaKey(opacity, 0f), new GradientAlphaKey(0f, 1f) });
            _trail.colorGradient = g;
        }

        private void LateUpdate()
        {
            Vector3 p = transform.position;
            float dt = Time.deltaTime;
            Vector3 step = p - _lastPos; step.y = 0f;
            bool moving = dt > 0f && step.magnitude / dt >= minSpeed;
            _lastPos = p;

            var state = GameServices.State;
            bool playing = _painter.Network != null && (state == null || !state.IsGameWon);
            _trail.emitting = playing && moving;
            _trail.transform.position = p + Vector3.up * lift;
        }
    }
}
