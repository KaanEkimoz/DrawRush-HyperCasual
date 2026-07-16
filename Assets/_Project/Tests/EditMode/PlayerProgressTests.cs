using NUnit.Framework;
using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    public sealed class PlayerProgressTests
    {
        private int _savedCoins;

        [SetUp]
        public void SetUp()
        {
            _savedCoins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", 0);
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.SetInt("Coins", _savedCoins);
            PlayerPrefs.Save();
        }

        [Test]
        public void AddCoins_Accumulates()
        {
            PlayerProgress.AddCoins(10);
            PlayerProgress.AddCoins(5);
            Assert.AreEqual(15, PlayerProgress.Coins);
        }

        [Test]
        public void AddCoins_IgnoresNonPositive()
        {
            PlayerProgress.AddCoins(10);
            PlayerProgress.AddCoins(0);
            PlayerProgress.AddCoins(-7);
            Assert.AreEqual(10, PlayerProgress.Coins);
        }

        [Test]
        public void AddCoins_RaisesCoinsChangedWithNewTotal()
        {
            int reported = -1;
            void Handler(int total) => reported = total;
            PlayerProgress.CoinsChanged += Handler;
            try
            {
                PlayerProgress.AddCoins(25);
            }
            finally
            {
                PlayerProgress.CoinsChanged -= Handler;
            }
            Assert.AreEqual(25, reported);
        }
    }
}
