using System;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Player-local persistent progress, backed by <see cref="PlayerPrefs"/> so it survives
    /// app restarts (per-device). Tracks whether the tutorial has been completed (shown once
    /// to a new player), the player's coin total (the win reward), and the level they reached
    /// so a returning player resumes instead of restarting the campaign.
    /// </summary>
    public static class PlayerProgress
    {
        private const string TutorialCompletedKey = "TutorialCompleted";
        private const string CoinsKey = "Coins";
        private const string LastLevelIndexKey = "LastLevelIndex";
        private const string LevelsPlayedKey = "LevelsPlayed";
        private const string LevelBagKey = "LevelBag";
        private const string LastLevelEnemiesKey = "LastLevelEnemies";

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

        /// <summary>Level-group index the player last reached, so a returning player resumes
        /// there. -1 means "never played" — callers fall back to the tutorial/first level.</summary>
        public static int LastLevelIndex
        {
            get => PlayerPrefs.GetInt(LastLevelIndexKey, -1);
            set
            {
                PlayerPrefs.SetInt(LastLevelIndexKey, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>How many non-tutorial levels the player has been served. Drives the position
        /// along the difficulty curve — not the same as LastLevelIndex, which is only *which*
        /// level, and which the shuffle-bag hands out in a different order every cycle.</summary>
        public static int LevelsPlayed
        {
            get => PlayerPrefs.GetInt(LevelsPlayedKey, 0);
            set
            {
                PlayerPrefs.SetInt(LevelsPlayedKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
            }
        }

        /// <summary>Levels left in the current shuffle-bag cycle, comma-separated, so quitting
        /// mid-cycle doesn't hand the player shapes they have already seen. Empty = draw refills.</summary>
        public static string LevelBag
        {
            get => PlayerPrefs.GetString(LevelBagKey, string.Empty);
            set
            {
                PlayerPrefs.SetString(LevelBagKey, value ?? string.Empty);
                PlayerPrefs.Save();
            }
        }

        /// <summary>Enemies the sequencer switched on for <see cref="LastLevelIndex"/>. Levels no
        /// longer carry a fixed enemy count — it is chosen per playthrough — so without this a
        /// relaunch would restore the level with every authored enemy live, turning a breather the
        /// player quit on into the hardest version of itself. -1 = unknown, leave the level as authored.</summary>
        public static int LastLevelEnemies
        {
            get => PlayerPrefs.GetInt(LastLevelEnemiesKey, -1);
            set
            {
                PlayerPrefs.SetInt(LastLevelEnemiesKey, value);
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
