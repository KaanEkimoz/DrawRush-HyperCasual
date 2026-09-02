using NUnit.Framework;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    /// <summary>Coin-number abbreviation: keeps the counter short (1.2K / 2.5M) instead of
    /// sprawling to eight digits, matching the reference art's compact style.</summary>
    public sealed class CoinHudTests
    {
        [Test]
        public void UnderAThousand_IsPlain()
        {
            Assert.AreEqual("0", CoinHud.Abbreviate(0));
            Assert.AreEqual("305", CoinHud.Abbreviate(305));
            Assert.AreEqual("999", CoinHud.Abbreviate(999));
        }

        [Test]
        public void Thousands_UseK_WithTrailingZeroTrimmed()
        {
            Assert.AreEqual("1.3K", CoinHud.Abbreviate(1250));
            Assert.AreEqual("5K", CoinHud.Abbreviate(5000));      // not "5.0K"
            Assert.AreEqual("127.5K", CoinHud.Abbreviate(127535));
        }

        [Test]
        public void Millions_UseM()
        {
            Assert.AreEqual("1M", CoinHud.Abbreviate(1_000_000));
            Assert.AreEqual("2.5M", CoinHud.Abbreviate(2_500_000));
        }
    }
}
