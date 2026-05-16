using System;
using UnityEngine;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Player health as a ScriptableObject so it can be referenced by UI, enemies,
    /// and save systems without Find/Tag lookups. Reset to <see cref="StartingValue"/>
    /// every scene load via GameBootstrap.
    ///
    /// API contract: <see cref="TakeDamage"/> and <see cref="Heal"/> both take a
    /// non-negative magnitude. Negative or zero is treated as a no-op. Heals after
    /// death are ignored — Died fires exactly once.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHealth", menuName = "DrawRush/Player/Player Health", order = 0)]
    public sealed class PlayerHealth : ScriptableObject
    {
        [SerializeField] private int startingValue = 3;

        /// <summary>Fired with the new value after every successful change.</summary>
        public event Action<int> Changed;

        /// <summary>Fired once when Current transitions from >0 to 0.</summary>
        public event Action Died;

        [NonSerialized] private int _current;

        public int Current => _current;
        public int StartingValue => startingValue;
        public bool IsAlive => _current > 0;

        public void ResetToStarting()
        {
            _current = startingValue;
            Changed?.Invoke(_current);
        }

        /// <summary>Reduces Current by <paramref name="amount"/>. Negative / zero ignored.
        /// Fires Died once when Current first reaches zero.</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || _current <= 0) return;
            _current = Mathf.Max(0, _current - amount);
            Changed?.Invoke(_current);
            if (_current == 0)
            {
                Died?.Invoke();
            }
        }

        /// <summary>Increases Current by <paramref name="amount"/> up to StartingValue.
        /// Negative / zero ignored. Ignored entirely after death.</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || _current <= 0) return;
            _current = Mathf.Min(startingValue, _current + amount);
            Changed?.Invoke(_current);
        }
    }
}
