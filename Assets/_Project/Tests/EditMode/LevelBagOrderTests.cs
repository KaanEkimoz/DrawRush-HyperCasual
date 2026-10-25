using NUnit.Framework;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    /// <summary>Locks in the fixed campaign order (Kaan: levels must read 1→2→3, not a shuffle).</summary>
    public sealed class LevelBagOrderTests
    {
        private static readonly int[] Levels = { 5, 1, 3, 2, 4 };

        [Test]
        public void PoolOfOne_GivesTheSameOrderEveryPlaythrough()
        {
            string first = Play(new System.Random(1));
            string second = Play(new System.Random(999));
            Assert.AreEqual(first, second);
        }

        [Test]
        public void PoolOfOne_BreaksTiesOnLowestIndex()
        {
            var bag = new LevelBag { CandidatePool = 1 };
            Assert.AreEqual(1, bag.Draw(0f, _ => 0f, Levels));
            Assert.AreEqual(2, bag.Draw(0f, _ => 0f, Levels));
        }

        private static string Play(System.Random rng)
        {
            var bag = new LevelBag(rng) { CandidatePool = 1 };
            var order = new System.Text.StringBuilder();
            for (int i = 0; i < Levels.Length; i++)
                order.Append(bag.Draw(i, level => level * 0.9f, Levels)).Append(',');
            return order.ToString();
        }
    }
}
