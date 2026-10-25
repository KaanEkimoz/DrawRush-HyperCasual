using System;
using System.Collections.Generic;
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
                case BoosterType.Magnet:
                    if (painter != null) painter.BandBoost = config.MagnetBandMultiplier;
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
                case BoosterType.Magnet:
                    if (painter != null) painter.BandBoost = 1f;
                    break;
            }
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
