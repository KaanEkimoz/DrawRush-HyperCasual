using System;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Booster tunables: what each booster does and for how long, and when and where pickups
    /// appear. Durations are the base values; shop upgrades add <see cref="UpgradeStep"/> per
    /// level on top (see <see cref="Duration"/>). Shockwave is instant: its "duration" is the stun.
    /// </summary>
    [CreateAssetMenu(fileName = "BoosterConfig", menuName = "DrawRush/Boosters/Booster Config", order = 0)]
    public sealed class BoosterConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public BoosterType type;
            [Tooltip("Seconds the effect lasts (shield: seconds before an unused shield fades).")]
            public float duration;
            [Tooltip("Seconds added per shop upgrade level.")]
            public float upgradeStep;
            [Tooltip("Pickup and HUD colour.")]
            public Color color;
            [Tooltip("Relative chance to be picked when a pickup spawns.")]
            public float weight;
            [Tooltip("Name shown over the pickup and on the HUD (empty = the type name).")]
            public string title;
            [Tooltip("One line telling what it does, shown when it is collected.")]
            public string hint;
        }

        [SerializeField] private Entry[] _entries =
        {
            new() { type = BoosterType.Shield, duration = 12f, upgradeStep = 3f, color = new Color(0.25f, 0.6f, 1f), weight = 1f },
            new() { type = BoosterType.Freeze, duration = 3f, upgradeStep = 0.5f, color = new Color(0.55f, 0.95f, 1f), weight = 1f },
            new() { type = BoosterType.Speed, duration = 5f, upgradeStep = 1f, color = new Color(1f, 0.85f, 0.15f), weight = 1f },
            new() { type = BoosterType.Shockwave, duration = 1.5f, upgradeStep = 0.4f, color = new Color(1f, 0.45f, 0.2f), weight = 1f },
        };

        [Header("Effects")]
        [SerializeField] private float _speedMultiplier = 1.35f;
        [Tooltip("Shockwave reach (m) around the player.")]
        [SerializeField] private float _shockwaveRadius = 6f;
        [Tooltip("How far (m) the shockwave throws each enemy it reaches.")]
        [SerializeField] private float _shockwavePush = 3.5f;
        [Tooltip("Invulnerable seconds after a shield breaks.")]
        [SerializeField] private float _shieldBreakGrace = 1f;

        [Header("Spawning")]
        [Tooltip("Seconds after a level starts before the first pickup.")]
        [SerializeField] private float _firstSpawnDelay = 6f;
        [Tooltip("Seconds between a pickup going (taken or expired) and the next one.")]
        [SerializeField] private Vector2 _spawnInterval = new(8f, 12f);
        [Tooltip("Seconds a pickup waits before vanishing.")]
        [SerializeField] private float _pickupLifetime = 7f;
        [Tooltip("The pickup blinks for this many final seconds.")]
        [SerializeField] private float _blinkSeconds = 2f;
        [Tooltip("Pickups never spawn closer than this (m) to an enemy.")]
        [SerializeField] private float _minEnemyDistance = 2.5f;
        [Tooltip("...or closer than this (m) to the player.")]
        [SerializeField] private float _minPlayerDistance = 2f;
        [Tooltip("At most this many different boosters run at once; a new one replaces the one closest to ending.")]
        [SerializeField] private int _maxActiveTypes = 2;
        [Tooltip("After a hit in the last N seconds, defensive boosters (shield, freeze) are this many times likelier.")]
        [SerializeField] private float _pityWindow = 10f;
        [SerializeField] private float _pityWeight = 3f;

        public float SpeedMultiplier => _speedMultiplier;
        public float ShockwaveRadius => _shockwaveRadius;
        public float ShockwavePush => _shockwavePush;
        public float ShieldBreakGrace => _shieldBreakGrace;
        public float FirstSpawnDelay => _firstSpawnDelay;
        public Vector2 SpawnInterval => _spawnInterval;
        public float PickupLifetime => _pickupLifetime;
        public float BlinkSeconds => _blinkSeconds;
        public float MinEnemyDistance => _minEnemyDistance;
        public float MinPlayerDistance => _minPlayerDistance;
        public int MaxActiveTypes => Mathf.Max(1, _maxActiveTypes);
        public float PityWindow => _pityWindow;
        public float PityWeight => _pityWeight;

        public Entry Get(BoosterType type)
        {
            foreach (Entry e in _entries) if (e.type == type) return e;
            return new Entry { type = type, duration = 5f, color = Color.white, weight = 1f };
        }

        /// <summary>Effect length for <paramref name="type"/> at shop upgrade <paramref name="level"/>.</summary>
        public float Duration(BoosterType type, int level)
        {
            Entry e = Get(type);
            return e.duration + e.upgradeStep * Mathf.Max(0, level);
        }

        public Color ColorOf(BoosterType type) => Get(type).color;

        public string TitleOf(BoosterType type)
        {
            string t = Get(type).title;
            return string.IsNullOrEmpty(t) ? type.ToString().ToUpperInvariant() : t;
        }

        public string HintOf(BoosterType type)
        {
            string h = Get(type).hint;
            if (!string.IsNullOrEmpty(h)) return h;
            return type switch
            {
                BoosterType.Shield => "Blocks the next hit",
                BoosterType.Freeze => "Enemies freeze in place",
                BoosterType.Speed => "You run faster",
                _ => "Blasts nearby enemies away",
            };
        }

        /// <summary>Weighted pick; defensive types weigh more when <paramref name="pity"/> is on.</summary>
        public BoosterType Pick(bool pity, Func<float> random01)
        {
            float total = 0f;
            foreach (Entry e in _entries) total += WeightOf(e, pity);
            float r = random01() * total;
            foreach (Entry e in _entries)
            {
                r -= WeightOf(e, pity);
                if (r <= 0f) return e.type;
            }
            return _entries.Length > 0 ? _entries[_entries.Length - 1].type : BoosterType.Shield;
        }

        private float WeightOf(Entry e, bool pity)
            => Mathf.Max(0f, e.weight) * (pity && (e.type == BoosterType.Shield || e.type == BoosterType.Freeze) ? _pityWeight : 1f);
    }
}
