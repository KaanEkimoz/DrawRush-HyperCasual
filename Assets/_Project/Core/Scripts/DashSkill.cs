using System;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// The dash, a skill bought in the shop: level 0 = not owned, then each level shortens the
    /// wait between dashes and makes the dash reach further. Saved in PlayerPrefs like the stats. A win pays about 12 coins, so
    /// unlocking takes a handful of wins.
    /// </summary>
    public static class DashSkill
    {
        private const string Key = "Skill_Dash";
        private static readonly int[] Prices = { 100, 180, 280 };
        private static readonly float[] Cooldowns = { 4f, 3.2f, 2.5f };
        private static readonly float[] Distances = { 3.4f, 4.1f, 4.8f };

        public static event Action Changed;

        public static int MaxLevel => Prices.Length;
        public static int Level => Mathf.Clamp(PlayerPrefs.GetInt(Key, 0), 0, MaxLevel);
        public static bool Owned => Level > 0;

        /// <summary>Seconds between dashes at <paramref name="level"/> (1..MaxLevel).</summary>
        public static float CooldownAt(int level) => Cooldowns[Mathf.Clamp(level, 1, MaxLevel) - 1];
        public static float Cooldown => CooldownAt(Level);

        /// <summary>Metres one dash covers at <paramref name="level"/>: each level reaches further.</summary>
        public static float DistanceAt(int level) => Distances[Mathf.Clamp(level, 1, MaxLevel) - 1];
        public static float Distance => DistanceAt(Level);

        /// <summary>Coins for the next level, or -1 when maxed.</summary>
        public static int NextPrice => Level < MaxLevel ? Prices[Level] : -1;
        public static System.Collections.Generic.IReadOnlyList<int> PriceSteps => Prices;

        public static bool TryUpgrade()
        {
            int price = NextPrice;
            if (price < 0 || !PlayerProgress.TrySpendCoins(price)) return false;
            PlayerPrefs.SetInt(Key, Level + 1);
            PlayerPrefs.Save();
            DrawRush.Analytics.Telemetry.Record(DrawRush.Analytics.Telemetry.Events.ShopPurchase, ("itemId", "skill_dash_" + Level), ("price", price), ("coinsLeft", PlayerProgress.Coins));
            Changed?.Invoke();
            return true;
        }

        /// <summary>The shop row's middle text.</summary>
        public static string Describe() => Owned ? $"{Distance:0.#}m · {Cooldown:0.#}s" : DrawRush.Localization.Loc.T("Locked");
    }
}
