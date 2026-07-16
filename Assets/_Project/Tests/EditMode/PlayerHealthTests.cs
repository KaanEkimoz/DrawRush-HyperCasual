using NUnit.Framework;
using UnityEngine;
using DrawRush.Player;

namespace DrawRush.Tests.EditMode
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
        public void TakeDamage_PositiveAmount_ReducesCurrent()
        {
            _health.TakeDamage(1);
            Assert.AreEqual(2, _health.Current);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void TakeDamage_NegativeOrZero_IsNoOp()
        {
            _health.TakeDamage(0);
            _health.TakeDamage(-5);
            Assert.AreEqual(3, _health.Current);
        }

        [Test]
        public void TakeDamage_DownToZero_RaisesDiedExactlyOnce()
        {
            int deathRaised = 0;
            _health.Died += () => deathRaised++;

            _health.TakeDamage(1);
            _health.TakeDamage(1);
            _health.TakeDamage(1);

            Assert.AreEqual(0, _health.Current);
            Assert.IsFalse(_health.IsAlive);
            Assert.AreEqual(1, deathRaised);
        }

        [Test]
        public void TakeDamage_AfterDeath_IsNoOp_AndDoesNotRefireDied()
        {
            _health.TakeDamage(10);
            int deathRaised = 0;
            _health.Died += () => deathRaised++;

            _health.TakeDamage(5);

            Assert.AreEqual(0, _health.Current);
            Assert.AreEqual(0, deathRaised);
        }

        [Test]
        public void TakeDamage_FiresChangedWithNewValue()
        {
            int? lastChanged = null;
            _health.Changed += v => lastChanged = v;

            _health.TakeDamage(1);

            Assert.AreEqual(2, lastChanged);
        }

        [Test]
        public void TakeDamage_NeverGoesNegative()
        {
            _health.TakeDamage(100);
            Assert.AreEqual(0, _health.Current);
        }

        [Test]
        public void Heal_PositiveAmount_IncreasesCurrent()
        {
            _health.TakeDamage(2);
            Assert.AreEqual(1, _health.Current);

            _health.Heal(1);

            Assert.AreEqual(2, _health.Current);
        }

        [Test]
        public void Heal_CapsAtStartingValue()
        {
            _health.Heal(100);
            Assert.AreEqual(3, _health.Current);
        }

        [Test]
        public void Heal_AfterDeath_IsNoOp()
        {
            _health.TakeDamage(10);

            _health.Heal(2);

            Assert.AreEqual(0, _health.Current);
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void Heal_NegativeOrZero_IsNoOp()
        {
            _health.TakeDamage(1); // Current = 2
            _health.Heal(0);
            _health.Heal(-5);
            Assert.AreEqual(2, _health.Current);
        }
    }
}
