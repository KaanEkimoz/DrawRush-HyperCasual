using System;
using System.Collections.Generic;
using System.Text;

namespace DrawRush.Core
{
    /// <summary>
    /// Draws levels without repeats: shuffle every level into a bag, pull them out one at a
    /// time, and only reshuffle once the bag is empty — so a player sees all N shapes before
    /// any shape comes round again. Adding levels just makes the bag bigger.
    ///
    /// The pull is not blind: each draw asks for a target difficulty and the bag hands back one
    /// of the closest matches, which is what lets the campaign follow a curve while staying
    /// varied. Ties (and near-ties) are broken randomly, so two players walking the same curve
    /// still get different orders.
    ///
    /// Plain C# on purpose — no Unity types — so the ordering rules are unit-testable.
    /// </summary>
    public sealed class LevelBag
    {
        private readonly List<int> _remaining = new();
        private readonly Random _rng;

        /// <summary>Candidates considered per draw: the N nearest to the target, picked between
        /// at random. 1 = strictly the closest (deterministic), higher = looser but more varied.</summary>
        public int CandidatePool { get; set; } = 3;

        public LevelBag(Random rng = null) => _rng = rng ?? new Random();

        /// <summary>Levels still unseen this cycle.</summary>
        public int Count => _remaining.Count;

        /// <summary>Refill with every drawable level (the caller excludes the tutorial).</summary>
        public void Refill(IEnumerable<int> indices)
        {
            _remaining.Clear();
            foreach (int i in indices) _remaining.Add(i);
        }

        /// <summary>
        /// Take the level nearest <paramref name="target"/> difficulty out of the bag. Refills
        /// automatically when empty, so the caller never has to track cycles. Returns -1 only if
        /// there is nothing to draw at all.
        /// </summary>
        public int Draw(float target, Func<int, float> difficultyOf, IEnumerable<int> refillWith)
        {
            if (_remaining.Count == 0) Refill(refillWith);
            if (_remaining.Count == 0) return -1;

            // Rank what's left by how close it sits to the target, then pick among the closest few
            // so the curve is honoured without the order being identical every playthrough.
            var ranked = new List<int>(_remaining);
            ranked.Sort((a, b) =>
                Math.Abs(difficultyOf(a) - target).CompareTo(Math.Abs(difficultyOf(b) - target)));

            int pool = Math.Max(1, Math.Min(CandidatePool, ranked.Count));
            int chosen = ranked[_rng.Next(pool)];
            _remaining.Remove(chosen);
            return chosen;
        }

        /// <summary>Comma-separated remaining indices, for PlayerPrefs.</summary>
        public string Serialize() => string.Join(",", _remaining);

        /// <summary>Restore a bag mid-cycle. Unparsable or empty input leaves the bag empty,
        /// which simply means the next Draw refills — never a hard failure.</summary>
        public void Deserialize(string data)
        {
            _remaining.Clear();
            if (string.IsNullOrEmpty(data)) return;
            foreach (string part in data.Split(','))
                if (int.TryParse(part, out int v)) _remaining.Add(v);
        }
    }
}
