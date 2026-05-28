using System;
using UnityEngine;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Player-local persistent progress, backed by <see cref="PlayerPrefs"/> so it survives
    /// app restarts (per-device). Tracks whether the tutorial has been completed (shown once
    /// to a new player) and the player's coin total (the win reward).
    /// </summary>
    public static class PlayerProgress
    {
        private const string TutorialCompletedKey = "TutorialCompleted";
        private const string CoinsKey = "Coins";

        /// <summary>Raised whenever the coin total changes, with the new total.</summary>
        public static event Action<int> CoinsChanged;

        /// <summary>True once the player has finished the tutorial level.</summary>
        public static bool TutorialCompleted
        {
            get => PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TutorialCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Total coins earned (the persistent win reward).</summary>
        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

        /// <summary>Add coins (clamped to >= 0 delta) and persist; raises CoinsChanged.</summary>
        public static void AddCoins(int amount)
        {
            if (amount <= 0) return;
            int total = Coins + amount;
            PlayerPrefs.SetInt(CoinsKey, total);
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(total);
        }
    }
}
