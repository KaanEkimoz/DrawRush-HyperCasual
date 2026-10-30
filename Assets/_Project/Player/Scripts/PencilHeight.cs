using DrawRush.Enemy;
using UnityEngine;

namespace DrawRush.Player
{
    /// <summary>
    /// Health shown on the hero itself: the pencil gets shorter with every hit and grows back with
    /// an ink pot. Replaces the heart counter, so the player reads their health without looking
    /// away from the action.
    /// </summary>
    public sealed class PencilHeight : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private HopAnimator pencil;
        [Tooltip("Height multiple with one health left. Full health is always 1.")]
        [SerializeField, Range(0.2f, 1f)] private float shortest = 0.5f;
        [Tooltip("How fast the height eases to its new value (per second).")]
        [SerializeField] private float ease = 6f;

        private float _target = 1f, _shown = 1f;

        /// <summary>Height multiple for <paramref name="current"/> of <paramref name="max"/> health.</summary>
        public static float HeightFor(int current, int max, float shortest)
        {
            if (max <= 1) return 1f;
            return Mathf.Lerp(shortest, 1f, Mathf.Clamp01((current - 1) / (float)(max - 1)));
        }

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
            if (value > 0) _target = HeightFor(value, health.StartingValue, shortest);
        }

        private void Update()
        {
            _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-ease * Time.deltaTime));
            if (pencil != null) pencil.HeightFactor = _shown;
        }
    }
}
