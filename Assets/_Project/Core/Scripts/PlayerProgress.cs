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
        private const string WonLevelKey = "WonLevelPendingAdvance";
        private const string LevelBagKey = "LevelBag";
        private const string LastLevelEnemiesKey = "LastLevelEnemies";
        private const string StarsKeyPrefix = "Stars_";
        private const string OwnedCosmeticsKey = "OwnedCosmetics";
        private const string EquippedCosmeticKey = "EquippedCosmetic";

        /// <summary>Raised whenever the coin total changes, with the new total.</summary>
        public static event Action<int> CoinsChanged;

        /// <summary>Raised with the newly-equipped cosmetic id when the player changes skins, so the
        /// player's visual can recolour without polling.</summary>
        public static event Action<string> CosmeticChanged;

        /// <summary>True if the player owns the skin with this id. The default skin (empty/asked-for
        /// default id) is always owned — the caller passes the library's DefaultId to check it.</summary>
        public static bool IsCosmeticOwned(string id, string defaultId)
        {
            if (string.IsNullOrEmpty(id) || id == defaultId) return true;
            string csv = PlayerPrefs.GetString(OwnedCosmeticsKey, string.Empty);
            foreach (var part in csv.Split(',')) if (part == id) return true;
            return false;
        }

        /// <summary>Mark a skin owned (idempotent).</summary>
        public static void OwnCosmetic(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            string csv = PlayerPrefs.GetString(OwnedCosmeticsKey, string.Empty);
            foreach (var part in csv.Split(',')) if (part == id) return;   // already owned
            csv = string.IsNullOrEmpty(csv) ? id : csv + "," + id;
            PlayerPrefs.SetString(OwnedCosmeticsKey, csv);
            PlayerPrefs.Save();
        }

        /// <summary>Currently-equipped skin id, or empty if none chosen yet (caller falls back to
        /// the library default).</summary>
        public static string EquippedCosmetic => PlayerPrefs.GetString(EquippedCosmeticKey, string.Empty);

        /// <summary>Equip a skin and notify listeners. No ownership check here — the shop gates that.</summary>
        public static void EquipCosmetic(string id)
        {
            PlayerPrefs.SetString(EquippedCosmeticKey, id ?? string.Empty);
            PlayerPrefs.Save();
            CosmeticChanged?.Invoke(id);
        }

        /// <summary>Best star rating (0–3) the player has ever earned on a given level group.
        /// 0 means never cleared. Keyed by level index so the shuffle-bag order doesn't matter.</summary>
        public static int BestStars(int levelIndex) => PlayerPrefs.GetInt(StarsKeyPrefix + levelIndex, 0);

        /// <summary>Record a clear at <paramref name="stars"/>, keeping only the player's best for
        /// that level. Returns true if this beat the previous best (a new record worth celebrating).</summary>
        public static bool RecordStars(int levelIndex, int stars)
        {
            stars = Mathf.Clamp(stars, 0, 3);
            if (stars <= BestStars(levelIndex)) return false;
            PlayerPrefs.SetInt(StarsKeyPrefix + levelIndex, stars);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Sum of best stars across levels 0..count-1 — the player's mastery total, and a
        /// natural place to gate later unlocks.</summary>
        public static int TotalStars(int levelCount)
        {
            int sum = 0;
            for (int i = 0; i < levelCount; i++) sum += BestStars(i);
            return sum;
        }

        /// <summary>Back to the first level (the tutorial), as on a fresh install: level position,
        /// difficulty curve and shape bag are cleared. Coins, skins, upgrades and stars stay.</summary>
        public static void ResetLevelProgress()
        {
            PlayerPrefs.SetInt(TutorialCompletedKey, 0);
            PlayerPrefs.SetInt(LastLevelIndexKey, -1);
            PlayerPrefs.SetInt(LevelsPlayedKey, 0);
            PlayerPrefs.SetInt(WonLevelKey, -1);
            PlayerPrefs.SetInt(LastLevelEnemiesKey, -1);
            PlayerPrefs.SetString(LevelBagKey, string.Empty);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// A fresh start, as on a new install: levels, stars (and so the collection), coins, skins,
        /// stat/dash/booster upgrades and the one-time hints are all cleared. Device-level choices
        /// stay: language, sound/music mutes, vibration and the ad-consent answer. Only the game's
        /// own keys are deleted, so ad/analytics SDK data in PlayerPrefs is left alone.
        /// </summary>
        public static void ResetEverything()
        {
            string[] keys =
            {
                TutorialCompletedKey, CoinsKey, LastLevelIndexKey, LevelsPlayedKey, WonLevelKey, LevelBagKey,
                LastLevelEnemiesKey, OwnedCosmeticsKey, EquippedCosmeticKey,
                "Skill_Dash", "DashHintShown", "DashPracticePending", "PetLevel", "BuddyLevel",
                "DrawRush.TrapTest", "DrawRush.TrapTest.LastLevel", "JoystickFixed",
            };
            foreach (string k in keys) PlayerPrefs.DeleteKey(k);
            for (int i = 0; i < 512; i++) PlayerPrefs.DeleteKey(StarsKeyPrefix + i);
            foreach (string stat in System.Enum.GetNames(typeof(PlayerStat))) PlayerPrefs.DeleteKey("Stat_" + stat);
            foreach (string b in System.Enum.GetNames(typeof(DrawRush.Boosters.BoosterType))) PlayerPrefs.DeleteKey("BoosterLevel_" + b);
            PlayerPrefs.DeleteKey("BoosterLevel_Magnet");
            PlayerPrefs.Save();
            CoinsChanged?.Invoke(0);
            CosmeticChanged?.Invoke(string.Empty);
        }

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

        /// <summary>Level index that was just won and paid but not yet advanced past (the player is
        /// on its win panel), or -1. Coins pay at the win while the advance happens on Next, so
        /// without this, quitting on the win panel booted back into the same level to pay again.</summary>
        public static int WonLevelPendingAdvance
        {
            get => PlayerPrefs.GetInt(WonLevelKey, -1);
            set
            {
                PlayerPrefs.SetInt(WonLevelKey, value);
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

        /// <summary>Spend coins if the player can afford it. Returns false and changes nothing when
        /// they can't, so the shop can gate a purchase on a single call. Raises CoinsChanged on
        /// success so the HUD counter ticks down.</summary>
        public static bool TrySpendCoins(int amount)
        {
            if (amount <= 0) return true;      // free items always "succeed"
            int total = Coins;
            if (total < amount) return false;
            total -= amount;
            // No Save here: the caller records what was bought and saves once, so a kill between
            // the two writes can never take the coins without handing over the item.
            PlayerPrefs.SetInt(CoinsKey, total);
            CoinsChanged?.Invoke(total);
            return true;
        }
    }
}
