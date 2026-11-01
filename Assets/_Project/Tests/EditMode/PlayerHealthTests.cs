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

        [Test]
        public void Combat_IgnoresHits_AfterTheLevelIsWon()
        {
            var go = new GameObject("CombatStub");
            var combat = go.AddComponent<PlayerCombat>();
            var so = new UnityEditor.SerializedObject(combat);
            so.FindProperty("health").objectReferenceValue = _health;
            so.ApplyModifiedPropertiesWithoutUndo();
            var state = ScriptableObject.CreateInstance<DrawRush.Core.GameState>();
            int damaged = 0;
            combat.Damaged += () => damaged++;
            try
            {
                DrawRush.Core.GameServices.Register(go.transform, go.transform, go.transform, null, state);
                state.IsGameWon = true;
                combat.TakeDamage(1);
                Assert.AreEqual(3, _health.Current);
                Assert.AreEqual(0, damaged);
            }
            finally
            {
                DrawRush.Core.GameServices.Clear();
                Object.DestroyImmediate(state);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Combat_IgnoresHits_AfterDeath()
        {
            var go = new GameObject("CombatStub");
            var combat = go.AddComponent<PlayerCombat>();
            var so = new UnityEditor.SerializedObject(combat);
            so.FindProperty("health").objectReferenceValue = _health;
            so.ApplyModifiedPropertiesWithoutUndo();
            int damaged = 0;
            combat.Damaged += () => damaged++;
            try
            {
                _health.TakeDamage(3);
                combat.TakeDamage(1);
                Assert.AreEqual(0, damaged);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void BonusMax_RaisesTheCapOnTheNextReset()
        {
            _health.BonusMax = 2;
            _health.ResetToStarting();
            Assert.AreEqual(5, _health.Current);
            _health.TakeDamage(1);
            _health.Heal(3);
            Assert.AreEqual(5, _health.Current);
        }

        [Test]
        public void EdgeFill_EraseCutsKeptPaintAndLeavesTheRest()
        {
            var fill = new DrawRush.Drawing.EdgeFill();
            fill.PaintRange(0f, 0.6f);
            fill.PaintPending(0.6f, 0.8f, 0f);
            Assert.IsTrue(fill.Erase(0.2f, 0.7f));
            Assert.AreEqual(0.3f, fill.Coverage, 0.001f);
            Assert.AreEqual(2, fill.Spans.Count);
            Assert.IsFalse(fill.Erase(0.3f, 0.4f));
        }

        [Test]
        public void DeskThemes_CycleThroughThePapers()
        {
            Assert.AreEqual(0, DrawRush.Core.DeskThemes.PaperIndex(0, 8));
            Assert.AreEqual(7, DrawRush.Core.DeskThemes.PaperIndex(7, 8));
            Assert.AreEqual(1, DrawRush.Core.DeskThemes.PaperIndex(9, 8));
        }
    }
}
