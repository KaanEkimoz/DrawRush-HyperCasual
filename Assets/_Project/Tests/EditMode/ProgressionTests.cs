using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using DrawRush.Boosters;
using DrawRush.Core;
using DrawRush.Enemy;

namespace DrawRush.Tests.EditMode
{
    /// <summary>The progression rules: which enemies each campaign level fields, how the shop is
    /// priced against the coin income, and how boosters scale with upgrades.</summary>
    public sealed class ProgressionTests
    {
        private GameObject _go;
        private EnemyTierDirector _director;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("TierDirector");
            _director = _go.AddComponent<EnemyTierDirector>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_go);

        private (int s, int t, int e, int d, bool intro) Plan(int level)
        {
            _director.Plan(level, out int s, out int t, out int e, out int d, out bool intro);
            return (s, t, e, d, intro);
        }

        [Test]
        public void EarlyLevels_FieldOnlyPlainEnemies()
        {
            for (int level = 1; level <= 5; level++)
                Assert.AreEqual((0, 0, 0, 0, false), Plan(level), $"level {level}");
        }

        [Test]
        public void EachNewType_DebutsAlone()
        {
            Assert.AreEqual((1, 0, 0, 0, true), Plan(6));
            Assert.AreEqual((0, 1, 0, 0, true), Plan(12));
            Assert.AreEqual((0, 0, 1, 0, true), Plan(18));
            Assert.AreEqual((0, 0, 0, 1, true), Plan(25));
        }

        [Test]
        public void AfterADebut_TheTypeStaysInTheMix()
        {
            Assert.AreEqual((1, 0, 0, 0, false), Plan(7));
            Assert.AreEqual((1, 1, 0, 0, false), Plan(13));
            Assert.AreEqual((2, 1, 1, 0, false), Plan(20));
            Assert.AreEqual((2, 2, 1, 1, false), Plan(26));
        }

        [Test]
        public void SecondLap_IsAHarderTierWithNoIntroductions()
        {
            Assert.AreEqual(0, _director.Lap(34));
            Assert.AreEqual(1, _director.Lap(35));
            var plan = Plan(40);
            Assert.IsFalse(plan.intro);
            Assert.AreEqual((3, 3, 2, 2), (plan.s, plan.t, plan.e, plan.d));
        }

        [Test]
        public void ShopPrices_ClimbAndTheFirstStepIsCheap()
        {
            const int averageWin = 12;   // LevelScore pays 6/12/20 by stars
            foreach (IReadOnlyList<int> steps in new[] { BoosterUpgrades.BoosterPriceSteps, BoosterUpgrades.PetPriceSteps, BoosterUpgrades.BuddyPriceSteps })
            {
                for (int i = 1; i < steps.Count; i++) Assert.Greater(steps[i], steps[i - 1]);
            }
            Assert.LessOrEqual(BoosterUpgrades.BoosterPriceSteps[0], 2 * averageWin, "first booster upgrade within two wins");
            Assert.AreEqual(BoosterUpgrades.MaxLevel, BoosterUpgrades.BoosterPriceSteps.Count);
            Assert.AreEqual(BoosterUpgrades.PetMaxLevel, BoosterUpgrades.PetPriceSteps.Count);
            Assert.AreEqual(BoosterUpgrades.BuddyMaxLevel, BoosterUpgrades.BuddyPriceSteps.Count);
            Assert.AreEqual(20, LevelScore.CoinsForStars(3));
        }

        [Test]
        public void BoosterDuration_GrowsWithUpgrades()
        {
            var config = ScriptableObject.CreateInstance<BoosterConfig>();
            try
            {
                Assert.AreEqual(12f, config.Duration(BoosterType.Shield, 0), 1e-4f);
                Assert.AreEqual(18f, config.Duration(BoosterType.Shield, 2), 1e-4f);
                Assert.AreEqual(3.5f, config.Duration(BoosterType.Freeze, 1), 1e-4f);
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void Pity_MakesDefensiveBoostersLikelier()
        {
            var config = ScriptableObject.CreateInstance<BoosterConfig>();
            try
            {
                int Defensive(bool pity)
                {
                    int n = 0;
                    for (int i = 0; i < 1000; i++)
                    {
                        float r = (i + 0.5f) / 1000f;
                        BoosterType t = config.Pick(pity, () => r);
                        if (t == BoosterType.Shield || t == BoosterType.Freeze) n++;
                    }
                    return n;
                }
                Assert.AreEqual(500, Defensive(false), 2);     // four equal weights: half are defensive
                Assert.AreEqual(750, Defensive(true), 2);      // 3x weight: (3+3)/(3+3+1+1)
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void NoEnemies_OnlySpeedComesUp()
        {
            var config = ScriptableObject.CreateInstance<BoosterConfig>();
            try
            {
                for (int i = 0; i < 100; i++)
                {
                    float r = (i + 0.5f) / 100f;
                    Assert.AreEqual(BoosterType.Speed, config.Pick(false, () => r, enemiesPresent: false));
                    Assert.AreEqual(BoosterType.Speed, config.Pick(true, () => r, enemiesPresent: false));
                }
            }
            finally { Object.DestroyImmediate(config); }
        }
    }
}
