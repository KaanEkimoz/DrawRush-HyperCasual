using NUnit.Framework;
using UnityEngine;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class PlayerHealthTests
    {
        private PlayerHealth _health;

        [SetUp]
        public void SetUp()
        {
            _health = ScriptableObject.CreateInstance<PlayerHealth>();
            var so = new UnityEditor.SerializedObject(_health);
            so.FindProperty("startingValue").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
            _health.ResetToStarting();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_health);
        }

        [Test]
        public void ResetToStarting_SetsCurrentToStartingValue()
        {
            Assert.AreEqual(3, _health.Current);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void Apply_NegativeDelta_ReducesCurrent()
        {
            _health.Apply(-1);
            Assert.AreEqual(2, _health.Current);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void Apply_DownToZero_RaisesDiedEvent()
        {
            int deathRaised = 0;
            _health.Died += () => deathRaised++;

            _health.Apply(-1);
            _health.Apply(-1);
            _health.Apply(-1);

            Assert.AreEqual(0, _health.Current);
            Assert.IsFalse(_health.IsAlive);
            Assert.AreEqual(1, deathRaised, "Died should fire exactly once when hp crosses to zero.");
        }

        [Test]
        public void Apply_AfterDeath_IsNoOp()
        {
            _health.Apply(-10);
            int deathRaised = 0;
            _health.Died += () => deathRaised++;

            _health.Apply(-5);

            Assert.AreEqual(0, _health.Current, "Health must stay at zero after death.");
            Assert.AreEqual(0, deathRaised, "Died must not re-fire after first death.");
        }

        [Test]
        public void Apply_FiresChangedEvent_WithNewValue()
        {
            int? lastChanged = null;
            _health.Changed += v => lastChanged = v;

            _health.Apply(-1);

            Assert.AreEqual(2, lastChanged);
        }

        [Test]
        public void Apply_NeverGoesNegative()
        {
            _health.Apply(-100);
            Assert.AreEqual(0, _health.Current);
        }
    }
}
