using System;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Runtime game-state holder. Replaces the legacy static GameManager.isGameWon flag.
    /// Reset by GameBootstrap on every scene load.
    /// </summary>
    [CreateAssetMenu(fileName = "GameState", menuName = "DrawRush/Core/Game State", order = 1)]
    public sealed class GameState : ScriptableObject
    {
        public event Action<bool> GameWonChanged;

        [NonSerialized] private bool _isGameWon;

        public bool IsGameWon
        {
            get => _isGameWon;
            set
            {
                if (_isGameWon == value) return;
                _isGameWon = value;
                GameWonChanged?.Invoke(value);
            }
        }

        public void Reset()
        {
            // Go through the property so GameWonChanged fires on a true->false reset —
            // shared subscribers (player dance/lock) have no scene reload to clear them.
            IsGameWon = false;
        }
    }
}
