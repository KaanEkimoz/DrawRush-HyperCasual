using NUnit.Framework;
using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Player;

namespace Studios208.DrawRush.Tests.EditMode
{
    [TestFixture]
    public sealed class GameServicesTests
    {
        [TearDown]
        public void TearDown()
        {
            GameServices.Clear();
        }

        [Test]
        public void IsReady_FalseWhenNothingRegistered()
        {
            GameServices.Clear();
            Assert.IsFalse(GameServices.IsReady);
        }

        [Test]
        public void Register_PopulatesAllFields()
        {
            var playerGo = new GameObject("PlayerStub");
            var trailGo = new GameObject("TrailStub");
            var camGo = new GameObject("CamStub");
            var config = ScriptableObject.CreateInstance<GameConfig>();
            var state = ScriptableObject.CreateInstance<GameState>();

            try
            {
                GameServices.Register(playerGo.transform, trailGo.transform, camGo.transform, config, state);

                Assert.AreSame(playerGo.transform, GameServices.Player);
                Assert.AreSame(trailGo.transform, GameServices.TrailPoint);
                Assert.AreSame(camGo.transform, GameServices.MainCamera);
                Assert.AreSame(config, GameServices.Config);
                Assert.AreSame(state, GameServices.State);
                Assert.IsTrue(GameServices.IsReady);
            }
            finally
            {
                Object.DestroyImmediate(playerGo);
                Object.DestroyImmediate(trailGo);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(state);
            }
        }

        [Test]
        public void Register_FiresServicesReadyEvent()
        {
            int readyCount = 0;
            void Handler() => readyCount++;
            GameServices.ServicesReady += Handler;
            try
            {
                var config = ScriptableObject.CreateInstance<GameConfig>();
                var state = ScriptableObject.CreateInstance<GameState>();
                var go = new GameObject("Stub");
                GameServices.Register(go.transform, null, null, config, state);
                Assert.AreEqual(1, readyCount);
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(state);
            }
            finally
            {
                GameServices.ServicesReady -= Handler;
            }
        }

        [Test]
        public void Clear_FiresServicesClearedEvent_AndNullsFields()
        {
            int clearedCount = 0;
            void Handler() => clearedCount++;
            GameServices.ServicesCleared += Handler;
            try
            {
                var go = new GameObject("Stub");
                var config = ScriptableObject.CreateInstance<GameConfig>();
                var state = ScriptableObject.CreateInstance<GameState>();
                GameServices.Register(go.transform, null, null, config, state);

                GameServices.Clear();

                Assert.IsNull(GameServices.Player);
                Assert.IsNull(GameServices.Config);
                Assert.IsNull(GameServices.State);
                Assert.IsFalse(GameServices.IsReady);
                Assert.AreEqual(1, clearedCount);

                Object.DestroyImmediate(go);
                Object.DestroyImmediate(config);
                Object.DestroyImmediate(state);
            }
            finally
            {
                GameServices.ServicesCleared -= Handler;
            }
        }
    }
}
