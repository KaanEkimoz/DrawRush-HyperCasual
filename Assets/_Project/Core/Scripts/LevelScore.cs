using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Turns how a level was cleared into a 1–3 star rating and the coin reward that goes with it.
    ///
    /// The skill this game actually tests is "close the shape without letting an enemy touch you" —
    /// enemies are the one thing that can end a run (which is why they weigh 3× a shape edge). So
    /// the rating reads off health, not time: a flawless clear is worth chasing, and a scrappy one
    /// still pays out. More stars pay more coins, which is what gives the shop something to pull
    /// against.
    ///
    /// Pure and Unity-free apart from Mathf, so the thresholds are unit-testable.
    /// </summary>
    public static class LevelScore
    {
        /// <summary>Stars for finishing with <paramref name="healthCurrent"/> of
        /// <paramref name="healthMax"/> left. Winning at all is worth one star; the last two are
        /// earned by not getting hit.</summary>
        public static int Evaluate(int healthCurrent, int healthMax)
        {
            if (healthMax <= 0) return 1;
            int cur = Mathf.Clamp(healthCurrent, 0, healthMax);
            if (cur >= healthMax) return 3;      // flawless — never touched
            if (cur * 2 >= healthMax) return 2;  // kept at least half
            return 1;                            // cleared it, barely
        }

        /// <summary>Coins for a clear at <paramref name="stars"/>. A 3-star clear pays well over
        /// three times a 1-star, so replaying for the top rating is the fast way to earn.</summary>
        public static int CoinsForStars(int stars)
        {
            switch (Mathf.Clamp(stars, 1, 3))
            {
                case 3: return 35;
                case 2: return 20;
                default: return 10;
            }
        }
    }
}
