using System;
using System.Collections;
using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Enemy;
using DrawRush.Player;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// The player's running boosters and their effects. Picking up a type that is already running
    /// refreshes it to full (never stacks); a new type past the active limit replaces the one
    /// closest to ending. Everything clears when a level starts.
    /// </summary>
    public sealed class PlayerBoosts : MonoBehaviour
    {
        [SerializeField] private BoosterConfig config;
        [SerializeField] private ThirdPersonMovement movement;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private FreeDrawPainter painter;
        [Tooltip("Transparent material for the shield bubble around the player.")]
        [SerializeField] private Material shieldMaterial;

        public struct Active { public BoosterType Type; public float Ends; public float Length; }

        private readonly List<Active> _active = new();
        private GameObject _bubble;
        private float _lastHitTime = -999f;
        private readonly List<BoosterType> _queued = new();

        /// <summary>Running boosters (time-left = Ends - Time.time).</summary>
        public IReadOnlyList<Active> Running => _active;

        public BoosterConfig Config => config;

        /// <summary>Raised whenever the running set changes.</summary>
        public event Action Changed;

        /// <summary>Raised when a booster is collected or granted, for the pickup announcement.</summary>
        public event Action<BoosterType> Granted;

        /// <summary>True if the player took a real hit within the config's pity window.</summary>
        public bool RecentlyHit => config != null && Time.time - _lastHitTime < config.PityWindow;

        private void Awake()
        {
            if (movement == null) movement = GetComponent<ThirdPersonMovement>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (painter == null) painter = GetComponent<FreeDrawPainter>();
        }

        private void OnEnable()
        {
            if (painter != null) painter.LevelBound += OnLevelBound;
            if (combat != null) { combat.ShieldBroken += OnShieldBroken; combat.Damaged += OnDamaged; }
        }

        private void OnDisable()
        {
            if (painter != null) painter.LevelBound -= OnLevelBound;
            if (combat != null) { combat.ShieldBroken -= OnShieldBroken; combat.Damaged -= OnDamaged; }
        }

        /// <summary>Start (or refresh) a booster at the player's upgrade level.</summary>
        public void Grant(BoosterType type)
        {
            if (config == null) return;
            float length = config.Duration(type, BoosterUpgrades.Level(type));

            if (type == BoosterType.Freeze)
                foreach (EnemyFollow e in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                    e.Freeze(length);

            if (type == BoosterType.Shockwave)
            {
                // Instant, so it never occupies a running slot or a HUD ring.
                Blast(length);
                Granted?.Invoke(type);
                return;
            }

            int existing = _active.FindIndex(a => a.Type == type);
            if (existing >= 0) _active.RemoveAt(existing);
            else if (_active.Count >= config.MaxActiveTypes)
            {
                int soonest = 0;
                for (int i = 1; i < _active.Count; i++) if (_active[i].Ends < _active[soonest].Ends) soonest = i;
                End(_active[soonest].Type);
                _active.RemoveAt(soonest);
            }
            _active.Add(new Active { Type = type, Ends = Time.time + length, Length = length });
            Begin(type);
            Changed?.Invoke();
            Granted?.Invoke(type);
        }

        private void Update()
        {
            bool changed = false;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (Time.time < _active[i].Ends) continue;
                End(_active[i].Type);
                _active.RemoveAt(i);
                changed = true;
            }
            if (_bubble != null && _bubble.activeSelf)
                _bubble.transform.localScale = Vector3.one * (2.2f + 0.08f * Mathf.Sin(Time.time * 6f));
            if (changed) Changed?.Invoke();
        }

        private void Begin(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Shield:
                    if (combat != null) combat.ShieldCharges = 1;
                    ShowBubble(true);
                    break;
                case BoosterType.Speed:
                    if (movement != null) movement.SpeedMultiplier = config.SpeedMultiplier;
                    break;
            }
        }

        private void End(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Shield:
                    if (combat != null) combat.ShieldCharges = 0;
                    ShowBubble(false);
                    break;
                case BoosterType.Speed:
                    if (movement != null) movement.SpeedMultiplier = 1f;
                    break;
            }
        }

        // Throw every enemy in reach straight away from the player and stun it; heavy enemies
        // fly half as far.
        private void Blast(float stunSeconds)
        {
            float radius = config.ShockwaveRadius;
            Vector3 p = transform.position;
            foreach (EnemyFollow e in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Vector3 away = e.transform.position - p;
                away.y = 0f;
                if (away.magnitude > radius) continue;
                if (away.sqrMagnitude < 1e-4f) away = transform.forward;
                e.Push(away, e.Unpushable ? config.ShockwavePush * 0.5f : config.ShockwavePush, evenHeavy: true);
                e.Freeze(stunSeconds);
            }
            Haptics.Medium();
            StartCoroutine(Ring(radius, config.ColorOf(BoosterType.Shockwave)));
        }

        private IEnumerator Ring(float radius, Color color)
        {
            const int segments = 48;
            const float seconds = 0.35f;
            var go = new GameObject("ShockwaveRing", typeof(LineRenderer));
            var lr = go.GetComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = segments;
            lr.alignment = LineAlignment.TransformZ;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Vector3 c = transform.position + Vector3.up * 0.05f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = t / seconds;
                float r = Mathf.Lerp(0.5f, radius, 1f - (1f - k) * (1f - k));
                for (int i = 0; i < segments; i++)
                {
                    float a = i * Mathf.PI * 2f / segments;
                    lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
                lr.widthMultiplier = Mathf.Lerp(0.6f, 0.15f, k);
                lr.startColor = lr.endColor = new Color(color.r, color.g, color.b, 1f - k);
                yield return null;
            }
            Destroy(lr.material);
            Destroy(go);
        }

        private void OnShieldBroken()
        {
            int i = _active.FindIndex(a => a.Type == BoosterType.Shield);
            if (i < 0) return;
            End(BoosterType.Shield);
            _active.RemoveAt(i);
            Changed?.Invoke();
        }

        private void OnDamaged() => _lastHitTime = Time.time;

        private void OnLevelBound(EdgeNetwork _)
        {
            foreach (Active a in _active) End(a.Type);
            _active.Clear();
            Changed?.Invoke();
            foreach (BoosterType t in _queued) Grant(t);
            _queued.Clear();
        }

        /// <summary>Start <paramref name="type"/> as soon as the next level (or retry) begins.</summary>
        public void GrantOnNextLevel(BoosterType type)
        {
            if (!_queued.Contains(type)) _queued.Add(type);
        }

        private void ShowBubble(bool on)
        {
            if (on && _bubble == null && shieldMaterial != null)
            {
                _bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _bubble.name = "ShieldBubble";
                Destroy(_bubble.GetComponent<Collider>());
                _bubble.transform.SetParent(transform, false);
                _bubble.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                var r = _bubble.GetComponent<Renderer>();
                r.sharedMaterial = shieldMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            if (_bubble != null) _bubble.SetActive(on);
        }
    }
}
