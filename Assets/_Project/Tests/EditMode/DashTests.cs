using NUnit.Framework;
using UnityEngine;
using DrawRush.Core;
using DrawRush.UI;

namespace DrawRush.Tests.EditMode
{
    /// <summary>The dash skill: what it costs, how its wait shrinks, and what counts as a double tap.</summary>
    public sealed class DashTests
    {
        [Test]
        public void Dash_IsBoughtThenGetsQuicker()
        {
            Assert.AreEqual(3, DashSkill.MaxLevel);
            for (int i = 1; i < DashSkill.PriceSteps.Count; i++) Assert.Greater(DashSkill.PriceSteps[i], DashSkill.PriceSteps[i - 1]);
            Assert.Greater(DashSkill.PriceSteps[0], 12 * 4, "unlocking takes a few wins");
            Assert.Greater(DashSkill.CooldownAt(1), DashSkill.CooldownAt(2));
            Assert.Greater(DashSkill.CooldownAt(2), DashSkill.CooldownAt(3));
            Assert.Less(DashSkill.DistanceAt(1), DashSkill.DistanceAt(2), "each level reaches further");
            Assert.Less(DashSkill.DistanceAt(2), DashSkill.DistanceAt(3));
        }

        [Test]
        public void DoubleTap_IsQuickAndInTheSamePlace()
        {
            var at = new Vector2(300f, 200f);
            Assert.IsTrue(FloatingJoystick.IsDoubleTap(1f, at, 1.2f, at + new Vector2(20f, 10f), 0.28f, 150f));
            Assert.IsFalse(FloatingJoystick.IsDoubleTap(1f, at, 1.4f, at, 0.28f, 150f), "too slow");
            Assert.IsFalse(FloatingJoystick.IsDoubleTap(1f, at, 1.1f, at + new Vector2(400f, 0f), 0.28f, 150f), "too far");
            Assert.IsFalse(FloatingJoystick.IsDoubleTap(-10f, at, 0.1f, at, 0.28f, 150f), "no lift before it");
        }
    }
}
