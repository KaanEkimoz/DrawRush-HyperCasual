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

        /// <summary>Stars earned on the win that just happened (1–3), set by WinCondition before it
        /// flips <see cref="IsGameWon"/> so the win panel can read it. Lives here because the win
        /// flow already funnels through this object; it is not persisted (PlayerProgress is).</summary>
        [NonSerialized] public int LastStars;

        /// <summary>True if that win set a new star record for the level — the panel can celebrate
        /// a "new best" differently from a repeat clear.</summary>
        [NonSerialized] public bool LastStarsWereRecord;

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
