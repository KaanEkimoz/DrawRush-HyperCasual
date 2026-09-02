using NUnit.Framework;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    /// <summary>
    /// Locks the star thresholds and their coin payouts. These are design numbers, not arithmetic,
    /// so the point of the tests is that a later tweak has to face them deliberately.
    /// </summary>
    public sealed class LevelScoreTests
    {
        [Test]
        public void FlawlessClear_IsThreeStars()
        {
            // Never touched = full health = the top rating. This is the thing worth replaying for.
            Assert.AreEqual(3, LevelScore.Evaluate(3, 3));
            Assert.AreEqual(3, LevelScore.Evaluate(5, 5));
        }

        [Test]
        public void KeepingAtLeastHalf_IsTwoStars()
        {
            Assert.AreEqual(2, LevelScore.Evaluate(2, 3));   // lost one of three
            Assert.AreEqual(2, LevelScore.Evaluate(2, 4));   // exactly half
            Assert.AreEqual(2, LevelScore.Evaluate(3, 4));
        }

        [Test]
        public void BarelySurviving_IsOneStar()
        {
            Assert.AreEqual(1, LevelScore.Evaluate(1, 3));   // one of three
            Assert.AreEqual(1, LevelScore.Evaluate(1, 4));   // under half
        }

        [Test]
        public void WinningAtAll_IsNeverZeroStars()
        {
            // The rating is only ever computed on a WIN, so the floor is one star — a cleared level
            // must never show as unrated.
            for (int max = 1; max <= 6; max++)
                for (int cur = 1; cur <= max; cur++)
                    Assert.GreaterOrEqual(LevelScore.Evaluate(cur, max), 1, $"cur={cur} max={max}");
        }

        [Test]
        public void DegenerateMax_DoesNotThrowAndReturnsAtLeastOne()
        {
            Assert.AreEqual(1, LevelScore.Evaluate(0, 0));
            Assert.AreEqual(1, LevelScore.Evaluate(5, 0));
        }

        [Test]
        public void MoreStarsPayStrictlyMoreCoins()
        {
            int one = LevelScore.CoinsForStars(1);
            int two = LevelScore.CoinsForStars(2);
            int three = LevelScore.CoinsForStars(3);
            Assert.Less(one, two);
            Assert.Less(two, three);
            // Three stars must be worth chasing — well over three 1-star clears, so replaying for
            // the top rating beats grinding easy levels.
            Assert.Greater(three, one * 3);
        }

        [Test]
        public void CoinsForStars_ClampsOutOfRange()
        {
            Assert.AreEqual(LevelScore.CoinsForStars(1), LevelScore.CoinsForStars(0));
            Assert.AreEqual(LevelScore.CoinsForStars(3), LevelScore.CoinsForStars(9));
        }
    }
}
