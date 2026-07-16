using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Scores how hard a level is and shapes the campaign's difficulty curve.
    ///
    /// A level's SHAPE cost is fixed by how it was authored (more edges = a longer draw under
    /// pressure; arcs are fiddlier to trace than straight runs). Its ENEMY cost is not — that is
    /// the dial we turn per playthrough, which is what lets a complex shape still serve as a
    /// breather when the curve wants one.
    /// </summary>
    public static class LevelDifficulty
    {
        public const float EdgeWeight = 1.0f;
        public const float ArcBonus = 0.5f;    // on top of the edge itself
        // An enemy is worth three whole edges. Extra edges only make the draw LONGER; an enemy
        // forces you to break off the rail and dodge, and it threatens every remaining edge —
        // it is the one factor that can actually end the run. This also makes enemies a coarse
        // dial by design: adding one is a real step up, not a nudge.
        public const float EnemyWeight = 3.0f;

        /// <summary>Intrinsic cost of drawing the shape, enemies excluded.</summary>
        public static float ShapeScore(Transform level)
        {
            if (level == null) return 0f;
            int edges = 0, arcs = 0;
            foreach (MonoBehaviour mb in level.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null || mb.GetType().Name != "DrawEdgeAuthor") continue;
                edges++;
                if (mb.transform.Find("WP") != null) arcs++;   // authored waypoint => curved edge
            }
            return edges * EdgeWeight + arcs * ArcBonus;
        }

        /// <summary>Enemies this level should field to land on <paramref name="target"/>, capped by
        /// how many were actually authored. A shape already past the target fields none — that is
        /// the "take the enemies away" lever that keeps low points reachable late in the bag.</summary>
        public static int EnemyBudget(float target, float shapeScore, int available)
        {
            int want = Mathf.RoundToInt((target - shapeScore) / EnemyWeight);
            return Mathf.Clamp(want, 0, Mathf.Max(0, available));
        }

        /// <summary>Where a beginner's first tooth tops out, as a fraction of the min..max range.
        /// The teeth get taller from here; they never get shorter.</summary>
        public const float FirstPeakFraction = 0.45f;

        /// <summary>
        /// Sawtooth: difficulty climbs across <paramref name="period"/> levels, then snaps straight
        /// back to <paramref name="min"/> — tension, then real relief, over and over. What grows
        /// with experience is the PEAK, not the floor: the drop stays a genuine breather forever,
        /// which is the whole point of a saw rather than a ramp.
        ///
        /// The floor deliberately does not creep up with <paramref name="played"/>. Letting it do
        /// so looks reasonable and is a trap twice over: the breathers quietly disappear, and the
        /// floor climbs past what most levels can even be dialled up to, so half the campaign ends
        /// up permanently under target.
        ///
        /// At played = 0 this returns exactly <paramref name="min"/> — the first level after the
        /// tutorial is always the gentlest thing in the bag, with no enemies.
        /// </summary>
        public static float SawtoothTarget(int played, int period, float min, float max, int rampLevels)
        {
            period = Mathf.Max(2, period);
            played = Mathf.Max(0, played);

            float phase = (played % period) / (float)(period - 1);  // 0 = the drop, 1 = this tooth's peak
            float firstPeak = Mathf.Lerp(min, max, FirstPeakFraction);
            float peak = Mathf.Lerp(firstPeak, max, Mathf.Clamp01(played / (float)Mathf.Max(1, rampLevels)));
            return Mathf.Lerp(min, peak, phase);
        }

        /// <summary>Enable only the first <paramref name="count"/> enemies under the level and turn
        /// the rest off. Authored enemies are reused rather than spawned, so positions stay hand-placed.</summary>
        public static void ApplyEnemyBudget(Transform level, int count)
        {
            Transform enemies = level != null ? level.Find("Enemies") : null;
            if (enemies == null) return;
            for (int i = 0; i < enemies.childCount; i++)
                enemies.GetChild(i).gameObject.SetActive(i < count);
        }

        /// <summary>How many enemies the level has to offer.</summary>
        public static int AvailableEnemies(Transform level)
        {
            Transform enemies = level != null ? level.Find("Enemies") : null;
            return enemies != null ? enemies.childCount : 0;
        }
    }
}
