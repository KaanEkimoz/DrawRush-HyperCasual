using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>Everything new a campaign level can introduce: enemy types, ground traps, desk
    /// accidents and the bosses.</summary>
    public enum Novelty
    {
        None,
        Stapler, Scissors, HolePunch, Eraser, Sharpener, Putty, Glue, Pushpin,
        Spikes, Saw, Roller,
        Cup, Ink, Gust, Ruler,
        BossEraser, BossStapler,
    }

    /// <summary>
    /// The campaign's rule book, in one place: when each enemy, trap and desk accident is introduced,
    /// and what a level of a given number may field. Plain C# with no scene access, so the whole
    /// campaign can be simulated and tested.
    ///
    /// The rules, from the 2026-11 plan:
    /// • The difficulty is a sawtooth of <see cref="Period"/> levels (1, 6, 11 ... are the breathers,
    ///   5, 10, 15 ... the peaks; every tenth level is a boss duel).
    /// • One new thing at a time, on the 2nd or 3rd level of a tooth, alone on the page: an enemy
    ///   debut has no traps and no desk accidents, a trap or accident debut has no enemies. The
    ///   level after it is the practice: the new thing again, with something already known.
    /// • A breather never has enemies or accidents, and has one trap once traps exist (Kaan's choice).
    /// • Once unlocked, every kind keeps coming back: kinds rotate level by level, and the newest
    ///   leads for the three levels after its debut.
    /// </summary>
    public static class Campaign
    {
        /// <summary>Levels per sawtooth tooth (LevelFlow.sawPeriod must match).</summary>
        public const int Period = 5;
        public const int BossEvery = 10;

        /// <summary>Difficulty points of one ground trap and of a level with desk accidents
        /// (an enemy is worth <see cref="LevelDifficulty.EnemyWeight"/>).</summary>
        public const float TrapWeight = 1.5f;
        public const float EventWeight = 1f;

        /// <summary>How many levels after its debut a new kind leads the line-up.</summary>
        public const int NewcomerLevels = 3;

        /// <summary>The introduction schedule (campaign level number, 1-based).</summary>
        public static readonly (int level, Novelty thing)[] Debuts =
        {
            (4, Novelty.Stapler),
            (7, Novelty.Spikes),
            (8, Novelty.Scissors),
            (9, Novelty.Cup),
            (12, Novelty.Saw),
            (13, Novelty.HolePunch),
            (17, Novelty.Ink),
            (18, Novelty.Eraser),
            (22, Novelty.Roller),
            (23, Novelty.Sharpener),
            (27, Novelty.Gust),
            (28, Novelty.Putty),
            (32, Novelty.Ruler),
            (33, Novelty.Glue),
            (37, Novelty.Pushpin),
        };

        public static readonly Novelty[] EnemyKinds =
            { Novelty.Stapler, Novelty.Scissors, Novelty.HolePunch, Novelty.Eraser, Novelty.Sharpener, Novelty.Putty, Novelty.Glue, Novelty.Pushpin };
        public static readonly Novelty[] TrapKinds = { Novelty.Spikes, Novelty.Saw, Novelty.Roller };
        public static readonly Novelty[] EventKinds = { Novelty.Cup, Novelty.Ink, Novelty.Gust, Novelty.Ruler };

        public static bool IsEnemy(Novelty n) => n >= Novelty.Stapler && n <= Novelty.Pushpin;
        public static bool IsTrap(Novelty n) => n >= Novelty.Spikes && n <= Novelty.Roller;
        public static bool IsEvent(Novelty n) => n >= Novelty.Cup && n <= Novelty.Ruler;

        /// <summary>0 = the breather at the foot of a tooth ... Period-1 = its peak.</summary>
        public static int Phase(int level) => (Mathf.Max(1, level) - 1) % Period;
        public static bool IsBreather(int level) => Phase(level) == 0;
        public static bool IsBoss(int level) => level > 0 && level % BossEvery == 0;
        /// <summary>Bosses alternate: the Giant Eraser on 10, 30, 50 ..., the Giant Stapler on 20, 40 ...</summary>
        public static bool IsStaplerBoss(int level) => IsBoss(level) && (level / BossEvery) % 2 == 0;

        /// <summary>What this level introduces (bosses included), or None.</summary>
        public static Novelty DebutAt(int level)
        {
            foreach (var d in Debuts) if (d.level == level) return d.thing;
            if (level == BossEvery) return Novelty.BossEraser;
            if (level == 2 * BossEvery) return Novelty.BossStapler;
            return Novelty.None;
        }

        /// <summary>The level a kind first appears on (int.MaxValue if never scheduled).</summary>
        public static int DebutLevel(Novelty thing)
        {
            foreach (var d in Debuts) if (d.thing == thing) return d.level;
            return int.MaxValue;
        }

        public static bool Unlocked(Novelty thing, int level) => level >= DebutLevel(thing);

        // ---------------------------------------------------------------- traps

        /// <summary>Ground traps on this level: none before the spikes exist, none in a boss duel or
        /// under another debut; one on breathers and on the gentle start of a tooth, more towards
        /// the peak, and more teeth in (at most 1 until level 15, 2 until 35, then 3).</summary>
        public static int TrapCount(int level)
        {
            if (!Unlocked(Novelty.Spikes, level) || IsBoss(level)) return 0;
            Novelty debut = DebutAt(level);
            if (IsTrap(debut)) return 1;
            if (debut != Novelty.None) return 0;
            int cap = level < 15 ? 1 : level < 35 ? 2 : 3;
            int phase = Phase(level);
            return phase <= 2 ? 1 : phase == 3 ? Mathf.Min(cap, 2) : cap;
        }

        /// <summary>Which trap kind takes slot <paramref name="slot"/> of the level.</summary>
        public static Novelty TrapKind(int level, int slot) => Pick(TrapKinds, level, slot);

        // ---------------------------------------------------------------- desk accidents

        /// <summary>Whether desk accidents run on this level at all.</summary>
        public static bool EventsOn(int level)
        {
            if (!Unlocked(Novelty.Cup, level) || IsBreather(level) || IsBoss(level)) return false;
            Novelty debut = DebutAt(level);
            return debut == Novelty.None || IsEvent(debut);
        }

        /// <summary>Whether this accident may be rolled on the level.</summary>
        public static bool EventAllowed(Novelty ev, int level) => EventsOn(level) && Unlocked(ev, level);

        // ---------------------------------------------------------------- enemies

        /// <summary>Most enemies this level may field before the difficulty budget has its say:
        /// none on breathers or under a trap / accident debut, exactly one for an enemy debut or a
        /// boss duel, otherwise two (three after level 50).</summary>
        public static int EnemyCap(int level)
        {
            if (!Unlocked(Novelty.Stapler, level) || IsBreather(level)) return 0;
            Novelty debut = DebutAt(level);
            if (IsTrap(debut) || IsEvent(debut)) return 0;
            if (IsEnemy(debut) || IsBoss(level)) return 1;
            return level <= LevelDifficulty.EarlyLevels ? LevelDifficulty.EarlyMaxEnemies : LevelDifficulty.MaxEnemies;
        }

        /// <summary>Fewest enemies the level fields: from the middle of a tooth up, at least one
        /// (enemies are the heart of the game; traps and accidents must not crowd them out).</summary>
        public static int EnemyFloor(int level)
        {
            if (EnemyCap(level) == 0 || IsBoss(level) || DebutAt(level) != Novelty.None) return 0;
            return Phase(level) >= 2 ? 1 : 0;
        }

        /// <summary>A debut is learnt on a simple shape: shapes scoring above this are ranked down.</summary>
        public const float DebutShapeMax = 9f;

        /// <summary>Which enemy kind slot <paramref name="slot"/> of the level wears.</summary>
        public static Novelty EnemyKind(int level, int slot) => Pick(EnemyKinds, level, slot);

        /// <summary>The difficulty curve's defaults (LevelFlow uses the same values).</summary>
        public const float MinTarget = 3f, MaxTarget = 21f;
        public const int RampLevels = 30;

        /// <summary>The sawtooth's target for campaign level <paramref name="level"/> (1-based).</summary>
        public static float Target(int level) =>
            LevelDifficulty.SawtoothTarget(Mathf.Max(0, level - 1), Period, MinTarget, MaxTarget, RampLevels);

        /// <summary>Traps for this level on a shape of this score: up to <see cref="TrapCount"/>,
        /// fewer when the shape itself is already hard (a complex drawing needs fewer traps to reach
        /// the target), never fewer than one once traps are on.</summary>
        public static int Traps(int level, float shapeScore)
        {
            int max = TrapCount(level);
            if (max <= 1) return max;
            float room = Target(level) - shapeScore - (EventsOn(level) ? EventWeight : 0f)
                         - EnemyFloor(level) * LevelDifficulty.EnemyWeight;
            return Mathf.Clamp(Mathf.FloorToInt(room / TrapWeight), 1, max);
        }

        /// <summary>Points the level's traps and accidents add, before any enemy.</summary>
        public static float FixedCost(int level, float shapeScore) =>
            Traps(level, shapeScore) * TrapWeight + (EventsOn(level) ? EventWeight : 0f);

        // A debut fills slot 0 with the new kind; for NewcomerLevels after it the newest still leads;
        // the other slots (and every other level) rotate through the unlocked kinds, one step a
        // level, so every kind keeps coming back and two slots of one level never share a kind
        // while there are enough kinds to go round.
        private static Novelty Pick(Novelty[] kinds, int level, int slot)
        {
            int unlocked = 0;
            Novelty newest = Novelty.None;
            int newestAt = -1;
            foreach (Novelty k in kinds)
            {
                if (!Unlocked(k, level)) continue;
                unlocked++;
                if (DebutLevel(k) > newestAt) { newestAt = DebutLevel(k); newest = k; }
            }
            if (unlocked == 0) return Novelty.None;
            bool leads = level - newestAt <= NewcomerLevels;
            if (slot == 0 && leads) return newest;
            int pool = leads ? unlocked - 1 : unlocked;   // the newest sits out when it leads
            if (pool == 0) return newest;
            int i = (level + slot) % pool;
            foreach (Novelty k in kinds)
            {
                if (!Unlocked(k, level)) continue;
                if (leads && k == newest) continue;   // it already holds slot 0
                if (i-- == 0) return k;
            }
            return newest;
        }
    }
}
