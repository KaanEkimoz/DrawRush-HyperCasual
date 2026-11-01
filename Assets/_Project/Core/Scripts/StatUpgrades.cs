using System;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>The hero stats the shop sells.</summary>
    public enum PlayerStat
    {
        Speed = 0,      // runs faster
        Health = 1,     // one more hit before the pencil runs out
        DrawArea = 2,   // paints from further off the line
    }

    /// <summary>
    /// Shop progression for the hero's stats, saved in PlayerPrefs. A win pays about 12 coins, so
    /// the first steps cost a few wins and the last ones are long-term goals. Health is priced
    /// highest: each level is a whole extra hit. Bought levels apply from the next level start.
    /// </summary>
    public static class StatUpgrades
    {
        private const string KeyPrefix = "Stat_";
        private const float SpeedPerLevel = 0.05f;
        private const float DrawAreaPerLevel = 0.1f;

        private static readonly int[] SpeedPrices = { 40, 80, 140, 220, 320 };
        private static readonly int[] HealthPrices = { 120, 300 };
        private static readonly int[] DrawAreaPrices = { 40, 80, 140, 220, 320 };

        /// <summary>Raised after any purchase so shop rows can refresh.</summary>
        public static event Action Changed;

        private static int[] Prices(PlayerStat stat) => stat switch
        {
            PlayerStat.Speed => SpeedPrices,
            PlayerStat.Health => HealthPrices,
            _ => DrawAreaPrices,
        };

        public static int MaxLevel(PlayerStat stat) => Prices(stat).Length;

        public static int Level(PlayerStat stat) =>
            Mathf.Clamp(PlayerPrefs.GetInt(KeyPrefix + stat, 0), 0, MaxLevel(stat));

        /// <summary>Coins for the next level of <paramref name="stat"/>, or -1 when maxed.</summary>
        public static int NextPrice(PlayerStat stat)
        {
            int l = Level(stat);
            return l < MaxLevel(stat) ? Prices(stat)[l] : -1;
        }

        public static bool TryUpgrade(PlayerStat stat)
        {
            int price = NextPrice(stat);
            if (price < 0 || !PlayerProgress.TrySpendCoins(price)) return false;
            PlayerPrefs.SetInt(KeyPrefix + stat, Level(stat) + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }

        public static float SpeedMultiplier => 1f + Level(PlayerStat.Speed) * SpeedPerLevel;
        public static int BonusHealth => Level(PlayerStat.Health);
        public static float DrawAreaMultiplier => 1f + Level(PlayerStat.DrawArea) * DrawAreaPerLevel;

        /// <summary>What the current level gives, for the shop row.</summary>
        public static string Describe(PlayerStat stat) => stat switch
        {
            PlayerStat.Speed => $"+{Level(stat) * 5}%",
            PlayerStat.Health => $"+{Level(stat)}",
            _ => $"+{Level(stat) * 10}%",
        };
    }
}
