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
        private readonly List<int> _recent = new();   // last drawn, newest last
        private int _known = -1;                      // level count the bag was built for (-1 = unknown)

        /// <summary>Levels drawn most recently are not drawn again right after a refill.</summary>
        public const int RecentWindow = 8;
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

            // Rank what's left by how close it sits to the target, then pick among the closest few.
            // Ties break on index so a pool of 1 gives the same fixed campaign order every time.
            // Straight after a refill the bag holds the shapes just played too: leave those out
            // while anything else is left, so the end of one cycle never repeats at the start of
            // the next.
            var ranked = new List<int>();
            foreach (int i in _remaining) if (!_recent.Contains(i)) ranked.Add(i);
            if (ranked.Count == 0) ranked.AddRange(_remaining);
            ranked.Sort((a, b) =>
            {
                int byGap = Math.Abs(difficultyOf(a) - target).CompareTo(Math.Abs(difficultyOf(b) - target));
                return byGap != 0 ? byGap : a.CompareTo(b);
            });

            int pool = Math.Max(1, Math.Min(CandidatePool, ranked.Count));
            int chosen = ranked[_rng.Next(pool)];
            _remaining.Remove(chosen);
            _recent.Remove(chosen);
            _recent.Add(chosen);
            while (_recent.Count > RecentWindow) _recent.RemoveAt(0);
            return chosen;
        }

        /// <summary>For PlayerPrefs: "remaining|recent|levelCount", each list comma-separated.</summary>
        public string Serialize() => string.Join(",", _remaining) + "|" + string.Join(",", _recent) + "|" + _known;

        /// <summary>Levels added since the bag was saved (indices from the old count up) join the
        /// cycle in progress. A bag from before this was tracked counts as built for 35 levels.</summary>
        public void AddNewLevels(int levelCount, int tutorialIndex)
        {
            if (_known < 0) _known = _remaining.Count == 0 ? levelCount : LegacyLevelCount;
            for (int i = _known; i < levelCount; i++)
                if (i != tutorialIndex && !_remaining.Contains(i)) _remaining.Add(i);
            _known = levelCount;
        }

        /// <summary>Level groups in the scene when saved bags did not record the count (1.5.0 and older).</summary>
        public const int LegacyLevelCount = 35;

        /// <summary>Restore a bag mid-cycle. Unparsable or empty input leaves the bag empty,
        /// which simply means the next Draw refills — never a hard failure.</summary>
        public void Deserialize(string data)
        {
            _remaining.Clear();
            _recent.Clear();
            _known = -1;
            if (string.IsNullOrEmpty(data)) return;
            string[] parts = data.Split('|');   // older saves: just the remaining list
            Parse(parts[0], _remaining);
            if (parts.Length > 1) Parse(parts[1], _recent);
            if (parts.Length > 2 && int.TryParse(parts[2], out int known)) _known = known;
        }

        private static void Parse(string list, List<int> into)
        {
            if (string.IsNullOrEmpty(list)) return;
            foreach (string part in list.Split(','))
                if (int.TryParse(part, out int v)) into.Add(v);
        }
    }
}
