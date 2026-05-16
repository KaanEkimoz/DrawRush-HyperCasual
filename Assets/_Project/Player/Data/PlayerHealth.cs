using System;
using UnityEngine;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Player health as a ScriptableObject so it can be referenced by UI, enemies,
    /// and bootstrap without Find/Tag lookups. Reset to <see cref="startingValue"/>
    /// every scene load via GameBootstrap.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHealth", menuName = "DrawRush/Player/Player Health", order = 0)]
    public sealed class PlayerHealth : ScriptableObject
    {
        [SerializeField] private int startingValue = 3;

        public event Action<int> Changed;
        public event Action Died;

        [NonSerialized] private int _current;

        public int Current => _current;
        public bool IsAlive => _current > 0;

        public void ResetToStarting()
        {
            _current = startingValue;
            Changed?.Invoke(_current);
        }

        public void Apply(int delta)
        {
            if (_current <= 0) return;
            _current = Mathf.Max(0, _current + delta);
            Changed?.Invoke(_current);
            if (_current == 0)
            {
                Died?.Invoke();
            }
        }
    }
}
