using System;
using DrawRush.Core;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Shop progression for boosters and the helpers, saved in PlayerPrefs. One currency (coins).
    /// A win pays 6/12/20 by stars (about 12 on average, 18 with the x2 ad), so the first upgrade
    /// costs about two wins and each later one about two to four: there is always something
    /// within reach without a grind.
    /// </summary>
    public static class BoosterUpgrades
    {
        public const int MaxLevel = 4;
        public const int PetMaxLevel = 3;

        private const string LevelKeyPrefix = "BoosterLevel_";
        private const string PetLevelKey = "PetLevel";   // 0 = not owned
        private const string BuddyLevelKey = "BuddyLevel";   // 0 = not owned
        public const int BuddyMaxLevel = 3;
        private static readonly int[] BuddyPrices = { 90, 160, 260 };

        private static readonly int[] UpgradePrices = { 20, 40, 70, 110 };
        private static readonly int[] PetPrices = { 60, 120, 200 };

        /// <summary>Raised after any purchase so shop rows and HUDs can refresh.</summary>
        public static event Action Changed;

        public static int Level(BoosterType type) => Mathf.Clamp(PlayerPrefs.GetInt(LevelKeyPrefix + type, 0), 0, MaxLevel);

        /// <summary>Coins for the next level of <paramref name="type"/>, or -1 when maxed.</summary>
        public static int NextPrice(BoosterType type)
        {
            int l = Level(type);
            return l < MaxLevel ? UpgradePrices[l] : -1;
        }

        public static bool TryUpgrade(BoosterType type)
        {
            int price = NextPrice(type);
            if (price < 0 || !PlayerProgress.TrySpendCoins(price)) return false;
            PlayerPrefs.SetInt(LevelKeyPrefix + type, Level(type) + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        /// <summary>Pet level: 0 = not owned, 1 = owned, higher = upgraded.</summary>
        public static int PetLevel => Mathf.Clamp(PlayerPrefs.GetInt(PetLevelKey, 0), 0, PetMaxLevel);

        public static int PetNextPrice => PetLevel < PetMaxLevel ? PetPrices[PetLevel] : -1;

        /// <summary>Brush Buddy level: 0 = not owned, 1 = owned, higher = upgraded.</summary>
        public static int BuddyLevel => Mathf.Clamp(PlayerPrefs.GetInt(BuddyLevelKey, 0), 0, BuddyMaxLevel);

        public static int BuddyNextPrice => BuddyLevel < BuddyMaxLevel ? BuddyPrices[BuddyLevel] : -1;

        public static bool TryUpgradeBuddy()
        {
            int price = BuddyNextPrice;
            if (price < 0 || !PlayerProgress.TrySpendCoins(price)) return false;
            PlayerPrefs.SetInt(BuddyLevelKey, BuddyLevel + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public static bool TryUpgradePet()
        {
            int price = PetNextPrice;
            if (price < 0 || !PlayerProgress.TrySpendCoins(price)) return false;
            PlayerPrefs.SetInt(PetLevelKey, PetLevel + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }
    }
}
