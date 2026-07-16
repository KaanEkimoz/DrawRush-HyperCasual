using NUnit.Framework;
using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class GameStateTests
    {
        private GameState _state;

        [SetUp]
        public void SetUp()
        {
            _state = ScriptableObject.CreateInstance<GameState>();
            _state.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_state);
        }

        [Test]
        public void Reset_LeavesIsGameWonFalse()
        {
            Assert.IsFalse(_state.IsGameWon);
        }

        [Test]
        public void SettingIsGameWonTrue_RaisesEventWithTrue()
        {
            bool? lastValue = null;
            _state.GameWonChanged += v => lastValue = v;

            _state.IsGameWon = true;

            Assert.IsTrue(_state.IsGameWon);
            Assert.IsTrue(lastValue.HasValue && lastValue.Value);
        }

        [Test]
        public void SettingSameValue_DoesNotRaiseEvent()
        {
            int raisedCount = 0;
            _state.GameWonChanged += _ => raisedCount++;

            _state.IsGameWon = false;
            _state.IsGameWon = false;

            Assert.AreEqual(0, raisedCount, "GameWonChanged should not fire when the value is unchanged.");
        }

        [Test]
        public void Reset_ClearsWonStateBackToFalse()
        {
            _state.IsGameWon = true;
            _state.Reset();
            Assert.IsFalse(_state.IsGameWon);
        }
    }
}
