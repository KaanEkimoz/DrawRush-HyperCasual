using System.Collections.Generic;
using NUnit.Framework;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    /// <summary>The campaign rule book and the sawtooth it produces, checked on paper.</summary>
    public sealed class CampaignTests
    {
        // The 34 shapes of 1.5.0 (score, authored enemies), so the sawtooth is tested on the real mix.
        private static readonly (float score, int enemies)[] RealShapes =
        {
            (5.6f, 2), (4.1f, 1), (6.0f, 1), (7.3f, 2), (5.0f, 2), (4.0f, 2), (7.0f, 2), (8.0f, 2), (4.0f, 2), (6.5f, 2),
            (9.0f, 3), (10.0f, 2), (12.0f, 3), (4.3f, 4), (12.0f, 3), (7.9f, 1), (9.2f, 5), (5.1f, 3), (7.0f, 4), (6.4f, 5),
            (6.1f, 4), (7.3f, 5), (12.9f, 4), (11.5f, 5), (16.1f, 3), (17.9f, 4), (15.9f, 5), (17.2f, 4), (5.1f, 3), (6.7f, 4),
            (6.2f, 5), (11.4f, 4), (5.4f, 5), (9.1f, 6),
        };

        private static List<CampaignSimulator.Shape> Shapes()
        {
            var list = new List<CampaignSimulator.Shape>();
            for (int i = 0; i < RealShapes.Length; i++)
                list.Add(new CampaignSimulator.Shape { Name = "S" + i, Score = RealShapes[i].score, Enemies = RealShapes[i].enemies });
            return list;
        }

        [Test]
        public void Debuts_AreOneAtATime_OnTheGentleSideOfATooth()
        {
            var seen = new HashSet<int>();
            foreach (var d in Campaign.Debuts)
            {
                Assert.IsTrue(seen.Add(d.level), $"two debuts on level {d.level}");
                Assert.IsFalse(Campaign.IsBoss(d.level), $"{d.thing} debuts on a boss level");
                int phase = Campaign.Phase(d.level);
                Assert.That(phase, Is.InRange(1, 3), $"{d.thing} debuts at phase {phase} (0 = breather, 4 = peak)");
            }
        }

        [Test]
        public void ADebut_HasThePageToItself()
        {
            foreach (var d in Campaign.Debuts)
            {
                int l = d.level;
                if (Campaign.IsEnemy(d.thing))
                {
                    Assert.AreEqual(0, Campaign.TrapCount(l), $"{d.thing}: no traps");
                    Assert.IsFalse(Campaign.EventsOn(l), $"{d.thing}: no accidents");
                    Assert.AreEqual(1, Campaign.EnemyCap(l), $"{d.thing}: alone");
                    Assert.AreEqual(d.thing, Campaign.EnemyKind(l, 0));
                }
                else if (Campaign.IsTrap(d.thing))
                {
                    Assert.AreEqual(1, Campaign.TrapCount(l), $"{d.thing}: one trap");
                    Assert.AreEqual(d.thing, Campaign.TrapKind(l, 0));
                    Assert.AreEqual(0, Campaign.EnemyCap(l), $"{d.thing}: no enemies");
                    Assert.IsFalse(Campaign.EventsOn(l), $"{d.thing}: no accidents");
                }
                else
                {
                    Assert.IsTrue(Campaign.EventsOn(l), $"{d.thing}: accidents on");
                    Assert.AreEqual(0, Campaign.EnemyCap(l), $"{d.thing}: no enemies");
                    Assert.AreEqual(0, Campaign.TrapCount(l), $"{d.thing}: no traps");
                }
            }
        }

        [Test]
        public void Breathers_HaveNoEnemiesNoAccidents_AndOneTrapOnceTrapsExist()
        {
            for (int l = 1; l <= 200; l += Campaign.Period)
            {
                Assert.IsTrue(Campaign.IsBreather(l));
                Assert.AreEqual(0, Campaign.EnemyCap(l), $"level {l}");
                Assert.IsFalse(Campaign.EventsOn(l), $"level {l}");
                Assert.AreEqual(Campaign.Unlocked(Novelty.Spikes, l) ? 1 : 0, Campaign.TrapCount(l), $"level {l}");
            }
        }

        [Test]
        public void BossLevels_AreDuels()
        {
            for (int l = 10; l <= 200; l += 10)
            {
                Assert.AreEqual(0, Campaign.TrapCount(l), $"level {l}");
                Assert.IsFalse(Campaign.EventsOn(l), $"level {l}");
                Assert.AreEqual(1, Campaign.EnemyCap(l), $"level {l}");
            }
            Assert.IsFalse(Campaign.IsStaplerBoss(10));
            Assert.IsTrue(Campaign.IsStaplerBoss(20));
        }

        [Test]
        public void TheFirstThreeLevels_AreForLearningToDraw()
        {
            for (int l = 1; l <= 3; l++)
            {
                Assert.AreEqual(0, Campaign.EnemyCap(l));
                Assert.AreEqual(0, Campaign.TrapCount(l));
                Assert.IsFalse(Campaign.EventsOn(l));
            }
        }

        [Test]
        public void ANewcomer_LeadsForThreeLevels()
        {
            foreach (var d in Campaign.Debuts)
            {
                if (!Campaign.IsEnemy(d.thing) && !Campaign.IsTrap(d.thing)) continue;
                for (int l = d.level + 1; l <= d.level + Campaign.NewcomerLevels; l++)
                {
                    Novelty first = Campaign.IsEnemy(d.thing) ? Campaign.EnemyKind(l, 0) : Campaign.TrapKind(l, 0);
                    // A later debut takes over the lead; only check while this is the newest.
                    bool newer = false;
                    foreach (var o in Campaign.Debuts)
                        if (o.level > d.level && o.level <= l && Campaign.IsEnemy(o.thing) == Campaign.IsEnemy(d.thing)
                            && Campaign.IsTrap(o.thing) == Campaign.IsTrap(d.thing)) newer = true;
                    if (!newer) Assert.AreEqual(d.thing, first, $"level {l}");
                }
            }
        }

        [Test]
        public void EveryUnlockedEnemy_KeepsComingBack_AndSlotsDoNotRepeatAKind()
        {
            for (int start = 40; start <= 160; start += 20)
            {
                var seen = new HashSet<Novelty>();
                for (int l = start; l < start + Campaign.EnemyKinds.Length + Campaign.NewcomerLevels; l++)
                {
                    seen.Add(Campaign.EnemyKind(l, 0));
                    seen.Add(Campaign.EnemyKind(l, 1));
                    Assert.AreNotEqual(Campaign.EnemyKind(l, 0), Campaign.EnemyKind(l, 1), $"level {l}");
                }
                foreach (Novelty k in Campaign.EnemyKinds) Assert.IsTrue(seen.Contains(k), $"{k} missing from levels {start}+");
            }
        }

        [Test]
        public void Sawtooth_OnTheRealShapes_DropsClimbsAndHitsItsTargets()
        {
            List<CampaignSimulator.Row> rows = CampaignSimulator.Run(Shapes(), 110);
            float gap = 0f;
            int counted = 0;
            var last = new Dictionary<int, int>();
            foreach (var r in rows)
            {
                if (last.TryGetValue(r.Index, out int prev))
                    Assert.Greater(r.Level - prev, LevelBag.RecentWindow, $"shape {r.Name} again on {r.Level} after {prev}");
                last[r.Index] = r.Level;
                if (r.Breather) Assert.AreEqual(0, r.Enemies, $"breather {r.Level} fields enemies");
                if (r.Debut == Novelty.None && !r.Boss) { gap += System.Math.Abs(r.Total - r.Target); counted++; }
            }
            Assert.Less(gap / counted, 2.5f, "mean distance between the curve and what levels deliver");
            // Each tooth: its breather is easier than its hardest level.
            for (int t = 0; t + Campaign.Period <= rows.Count; t += Campaign.Period)
            {
                float top = 0f;
                for (int k = 1; k < Campaign.Period; k++) top = System.Math.Max(top, rows[t + k].Total);
                Assert.Less(rows[t].Total, top, $"tooth starting at level {rows[t].Level}: the drop is not a drop");
            }
        }

        [Test]
        public void Bag_DoesNotRepeatTheLastShapesRightAfterARefill()
        {
            var pool = new Dictionary<int, float>();
            for (int i = 0; i < 12; i++) pool[i] = i;
            var bag = new LevelBag(new System.Random(3)) { CandidatePool = 1 };
            var order = new List<int>();
            for (int n = 0; n < 36; n++) order.Add(bag.Draw(n % 2 == 0 ? 0f : 11f, x => pool[x], pool.Keys));
            for (int i = 0; i < order.Count; i++)
                for (int j = i + 1; j < order.Count && j - i <= LevelBag.RecentWindow; j++)
                    Assert.AreNotEqual(order[i], order[j], $"draws {i} and {j}");
        }

        [Test]
        public void Bag_LetsShapesFromAnUpdateJoinTheCycle()
        {
            var bag = new LevelBag();
            bag.Deserialize("3,7,9");   // a 1.5.0 save: 35 groups, these three left
            bag.AddNewLevels(levelCount: 38, tutorialIndex: 0);
            Assert.AreEqual(6, bag.Count, "35, 36 and 37 join");
            bag.AddNewLevels(levelCount: 38, tutorialIndex: 0);
            Assert.AreEqual(6, bag.Count, "nothing added twice");
            var fresh = new LevelBag();
            fresh.AddNewLevels(levelCount: 38, tutorialIndex: 0);
            Assert.AreEqual(0, fresh.Count, "an empty bag refills with everything anyway");
        }
    }
}
