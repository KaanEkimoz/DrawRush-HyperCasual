using DrawRush.Enemy;
using UnityEngine;

namespace DrawRush.Player
{
    /// <summary>
    /// Health shown on the hero itself: the pencil gets shorter with every hit and grows back with
    /// an ink pot. Replaces the heart counter, so the player reads their health without looking
    /// away from the action. A shorter pencil is also a faster one, which gives a losing player a
    /// little more room to escape.
    /// </summary>
    public sealed class PencilHeight : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private HopAnimator pencil;
        [Tooltip("Height multiple with one health left. Full health is always 1.")]
        [SerializeField, Range(0.2f, 1f)] private float shortest = 0.5f;
        [Tooltip("How fast the height eases to its new value (per second).")]
        [SerializeField] private float ease = 6f;

        [Tooltip("Extra speed for each hit taken (0.05 = +5% after one hit, +10% after two).")]
        [SerializeField, Range(0f, 0.5f)] private float speedPerHit = 0.05f;

        private ThirdPersonMovement _movement;
        private float _target = 1f, _shown = 1f;

        /// <summary>Height multiple for <paramref name="current"/> of <paramref name="max"/> health.</summary>
        public static float HeightFor(int current, int max, float shortest)
        {
            if (max <= 1) return 1f;
            return Mathf.Lerp(shortest, 1f, Mathf.Clamp01((current - 1) / (float)(max - 1)));
        }

        /// <summary>Speed multiple with <paramref name="current"/> of <paramref name="max"/> health.</summary>
        public static float SpeedFor(int current, int max, float perHit) =>
            1f + Mathf.Max(0, max - Mathf.Max(1, current)) * perHit;

        private void Awake() => _movement = GetComponent<ThirdPersonMovement>();

        private void OnEnable()
        {
            if (health == null) return;
            health.Changed += OnChanged;
            OnChanged(health.Current > 0 ? health.Current : health.StartingValue);
            _shown = _target;
        }

        private void OnDisable()
        {
            if (health != null) health.Changed -= OnChanged;
        }

        private void OnChanged(int value)
        {
            if (value <= 0) return;
            _target = HeightFor(value, health.StartingValue, shortest);
            if (_movement != null) _movement.HeightSpeedMultiplier = SpeedFor(value, health.StartingValue, speedPerHit);
        }

        /// <summary>Jump straight to the height for the current health. The level-start countdown
        /// freezes time, so easing there would show last level's short pencil until "GO".</summary>
        public void Snap()
        {
            if (health != null && health.Current > 0) OnChanged(health.Current);
            _shown = _target;
            if (pencil != null) pencil.HeightFactor = _shown;
        }

        private void Update()
        {
            _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-ease * Time.deltaTime));
            if (pencil != null) pencil.HeightFactor = _shown;
        }
    }
}
