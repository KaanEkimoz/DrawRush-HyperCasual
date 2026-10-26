using DrawRush.Core;
using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// A thin paint trail behind the player that fades in a couple of seconds, wherever they walk.
    /// It is only feedback ("I am the brush"): coverage still comes from the snapped paint on the
    /// template, so the shape stays clean while every step still leaves a mark. From the redesign
    /// research: off-template paint is cosmetic and fades. While the brush is actually laying
    /// paint on the shape a soft glow shows under it (the "emissive head" of the paint), and a
    /// speed boost turns the trail into a wide streak in the boost's colour.
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

        [Header("Brush glow")]
        [Tooltip("Glow diameter (m) under the brush while it paints the shape.")]
        [SerializeField] private float glowSize = 1.1f;
        [Tooltip("Glow pulses per second.")]
        [SerializeField] private float glowPulse = 3f;

        [Header("Speed streak")]
        [Tooltip("Trail width multiple while a speed boost runs.")]
        [SerializeField] private float boostWidthScale = 2.2f;

        private FreeDrawPainter _painter;
        private TrailRenderer _trail;
        private Vector3 _lastPos;
        private Color _paintColor;
        private Color? _boostColor;
        private Transform _glow;
        private Material _glowMaterial;
        private float _glowAlpha;

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
            _paintColor = color;
            ApplyLook();
            _trail.emitting = false;
            BuildGlow();
        }

        // A flat soft disc, generated so it needs no texture asset.
        private void BuildGlow()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d)));
                }
            tex.Apply();
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "BrushGlow";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = Vector3.one * glowSize;
            _glowMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Auto_BrushGlow", mainTexture = tex };
            var mr = quad.GetComponent<MeshRenderer>();
            mr.sharedMaterial = _glowMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            _glow = quad.transform;
            _glow.gameObject.SetActive(false);
        }

        /// <summary>Speed boost on (its colour) or off (null): the trail becomes a wide streak.</summary>
        public void SetBoost(Color? boostColor)
        {
            _boostColor = boostColor;
            ApplyLook();
        }

        private void ApplyLook()
        {
            SetColor(_boostColor ?? _paintColor);
            _trail.widthMultiplier = width * (_boostColor.HasValue ? boostWidthScale : 1f);
        }

        private void OnEnable() => _painter.LevelBound += OnLevelBound;
        private void OnDisable() => _painter.LevelBound -= OnLevelBound;

        private void OnDestroy()
        {
            if (_glow != null) { Destroy(_glowMaterial.mainTexture); Destroy(_glowMaterial); Destroy(_glow.gameObject); }
            if (_trail == null) return;
            Destroy(_trail.material);
            Destroy(_trail.gameObject);
        }

        // A new level (or a retry) teleports the player: never draw a streak across the map.
        private void OnLevelBound(EdgeNetwork network)
        {
            // Same colour as the line being painted, so the trail reads as that paint.
            if (network != null && network.Edges.Count > 0) { _paintColor = network.ColorOf(network.Edges[0]); ApplyLook(); }
            _lastPos = transform.position;
            _trail.transform.position = _lastPos + Vector3.up * lift;
            _trail.Clear();
        }

        private void UpdateGlow(Vector3 p, bool on)
        {
            if (_glow == null) return;
            _glowAlpha = Mathf.MoveTowards(_glowAlpha, on ? 1f : 0f, Time.deltaTime * 6f);
            bool show = _glowAlpha > 0.01f;
            if (_glow.gameObject.activeSelf != show) _glow.gameObject.SetActive(show);
            if (!show) return;
            _glow.position = p + Vector3.up * (lift * 0.5f);
            float pulse = 0.85f + 0.15f * Mathf.Sin(Time.time * glowPulse * 2f * Mathf.PI);
            _glow.localScale = Vector3.one * glowSize * pulse;
            Color c = Color.Lerp(_paintColor, Color.white, 0.35f);
            c.a = 0.7f * _glowAlpha;
            _glowMaterial.color = c;
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
            UpdateGlow(p, playing && _painter.IsPainting);
        }
    }
}
