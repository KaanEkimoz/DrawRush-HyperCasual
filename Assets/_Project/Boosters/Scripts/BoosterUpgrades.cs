using System;
using DrawRush.Core;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Shop progression for boosters and the pet, saved in PlayerPrefs. One currency (coins);
    /// prices climb about 2.5x per level so there is a buy moment every few levels without a
    /// grind (coin income is 6/12/20 per win by stars).
    /// </summary>
    public static class BoosterUpgrades
    {
        public const int MaxLevel = 4;
        public const int PetMaxLevel = 3;

        private const string LevelKeyPrefix = "BoosterLevel_";
        private const string PetLevelKey = "PetLevel";   // 0 = not owned

        private static readonly int[] UpgradePrices = { 30, 75, 180, 420 };
        private static readonly int[] PetPrices = { 120, 260, 520 };

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
