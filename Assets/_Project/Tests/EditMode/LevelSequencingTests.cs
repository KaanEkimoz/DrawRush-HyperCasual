using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Tests.EditMode
{
    /// <summary>
    /// Guards the level sequencer: the shuffle-bag's no-repeat promise, and the shape of the
    /// difficulty curve. Several of these encode design calls rather than arithmetic — they exist
    /// so a later tweak to the weights can't quietly undo them.
    /// </summary>
    public sealed class LevelSequencingTests
    {
        private const int Period = 5;
        private const float Min = 3f;
        private const float Max = 21f;
        private const int Ramp = 30;

        // A flat pool with a spread of difficulties, keyed by index.
        private static Dictionary<int, float> Pool(int count)
            => Enumerable.Range(0, count).ToDictionary(i => i, i => (float)i);

        // ---------- LevelBag ----------

        [Test]
        public void Draw_NeverRepeatsUntilEveryLevelHasBeenSeen()
        {
            var pool = Pool(10);
            var bag = new LevelBag(new System.Random(1)) { CandidatePool = 3 };

            var drawn = new List<int>();
            for (int i = 0; i < 10; i++)
                drawn.Add(bag.Draw(5f, x => pool[x], pool.Keys));

            CollectionAssert.AllItemsAreUnique(drawn);
            CollectionAssert.AreEquivalent(pool.Keys, drawn);
        }

        [Test]
        public void Draw_RefillsItselfOnceTheBagIsEmpty()
        {
            var pool = Pool(4);
            var bag = new LevelBag(new System.Random(2)) { CandidatePool = 2 };

            for (int i = 0; i < 4; i++) bag.Draw(2f, x => pool[x], pool.Keys);
            Assert.AreEqual(0, bag.Count, "bag should be spent");

            // The 5th draw must still return a real level — the cycle restarts on its own.
            Assert.That(bag.Draw(2f, x => pool[x], pool.Keys), Is.InRange(0, 3));
            Assert.AreEqual(3, bag.Count, "refilled with 4, one of them drawn");
        }

        [Test]
        public void Draw_WithASingleCandidate_TakesTheLevelNearestTheTarget()
        {
            var pool = Pool(10);
            var bag = new LevelBag(new System.Random(3)) { CandidatePool = 1 };

            Assert.AreEqual(7, bag.Draw(7.2f, x => pool[x], pool.Keys));
            Assert.AreEqual(0, bag.Draw(-5f, x => pool[x], pool.Keys), "clamps to the closest, not a failure");
        }

        [Test]
        public void Draw_WithNothingToDrawOrRefillWith_ReturnsMinusOne()
        {
            var bag = new LevelBag(new System.Random(4));
            Assert.AreEqual(-1, bag.Draw(1f, _ => 0f, new int[0]));
        }

        [Test]
        public void SerializeRoundTrip_PreservesTheRestOfTheCycle()
        {
            var pool = Pool(8);
            var bag = new LevelBag(new System.Random(5)) { CandidatePool = 1 };
            bag.Draw(3f, x => pool[x], pool.Keys);
            bag.Draw(6f, x => pool[x], pool.Keys);

            var restored = new LevelBag(new System.Random(5));
            restored.Deserialize(bag.Serialize());

            Assert.AreEqual(bag.Count, restored.Count);
            Assert.AreEqual(bag.Serialize(), restored.Serialize());
        }

        [Test]
        public void Deserialize_OnGarbageOrEmpty_LeavesAnEmptyBagRatherThanThrowing()
        {
            var bag = new LevelBag(new System.Random(6));
            Assert.DoesNotThrow(() => bag.Deserialize("4,,nonsense,7"));
            Assert.AreEqual(2, bag.Count, "keeps the parsable entries, drops the rest");

            Assert.DoesNotThrow(() => bag.Deserialize(""));
            Assert.AreEqual(0, bag.Count, "empty save = next draw refills");
        }

        // ---------- The curve ----------

        [Test]
        public void FirstLevelAfterTheTutorial_SitsAtTheVeryBottomOfTheCurve()
        {
            // Kaan hit a 3-enemy star straight after the tutorial. played = 0 must be the floor,
            // which is what makes the bag pick the simplest shape and budget it zero enemies.
            Assert.AreEqual(Min, LevelDifficulty.SawtoothTarget(0, Period, Min, Max, Ramp), 0.001f);
            Assert.AreEqual(0, LevelDifficulty.EnemyBudget(Min, Min, available: 3));
        }

        [Test]
        public void EveryToothStartsBackAtTheFloor_NoMatterHowDeepThePlayerIs()
        {
            // The regression that matters: an earlier version raised the floor with experience, so
            // past ~30 levels the breathers vanished and the saw flattened into a plateau.
            for (int played = 0; played < 400; played += Period)
                Assert.AreEqual(Min, LevelDifficulty.SawtoothTarget(played, Period, Min, Max, Ramp), 0.001f,
                    $"tooth starting at played={played} should drop all the way back to the floor");
        }

        [Test]
        public void TeethGetTallerWithExperience_AndStopAtMax()
        {
            float beginnerPeak = LevelDifficulty.SawtoothTarget(Period - 1, Period, Min, Max, Ramp);
            float veteranPeak = LevelDifficulty.SawtoothTarget(Ramp + Period - 1, Period, Min, Max, Ramp);

            Assert.Less(beginnerPeak, veteranPeak, "a beginner's peak must be gentler than a veteran's");
            Assert.AreEqual(Max, veteranPeak, 0.001f, "past the ramp the teeth reach the full range");
            Assert.AreEqual(Max, LevelDifficulty.SawtoothTarget(Ramp * 10 + Period - 1, Period, Min, Max, Ramp), 0.001f,
                "and never overshoot it");
        }

        [Test]
        public void CurveStaysInsideMinMax_ForEveryStepOfALongCampaign()
        {
            for (int played = 0; played < 400; played++)
                Assert.That(LevelDifficulty.SawtoothTarget(played, Period, Min, Max, Ramp),
                    Is.InRange(Min, Max), $"played={played}");
        }

        [Test]
        public void SawtoothTarget_SurvivesNonsenseTuning()
        {
            Assert.DoesNotThrow(() => LevelDifficulty.SawtoothTarget(-5, 0, Min, Max, 0));
            Assert.That(LevelDifficulty.SawtoothTarget(-5, 0, Min, Max, 0), Is.InRange(Min, Max));
        }

        // ---------- The enemy dial ----------

        [Test]
        public void AnEnemyOutweighsThreeEdges()
        {
            // Kaan's call: an enemy is "gerçekten zorlayıcı bir etmen" — it forces you off the rail
            // and can end the run, where extra edges only make the draw longer. Encoded so a later
            // weight tweak has to face this test.
            Assert.AreEqual(3f * LevelDifficulty.EdgeWeight, LevelDifficulty.EnemyWeight, 0.001f);
        }

        [Test]
        public void AShapeAlreadyPastTheTarget_FieldsNoEnemies()
        {
            // "gerekirse düşmanı çıkart, bazen zorluk düşük olsun" — the breather at the bottom of
            // each tooth only exists because a complex shape can be stripped of its enemies.
            Assert.AreEqual(0, LevelDifficulty.EnemyBudget(target: 4f, shapeScore: 15f, available: 5));
        }

        [Test]
        public void EnemyBudget_NeverExceedsWhatWasActuallyAuthored()
        {
            // Enemies are hand-placed and reused, never spawned — asking for more than exist would
            // silently under-deliver the target rather than error, so cap it here.
            Assert.AreEqual(2, LevelDifficulty.EnemyBudget(target: 100f, shapeScore: 3f, available: 2));
            Assert.AreEqual(0, LevelDifficulty.EnemyBudget(target: 100f, shapeScore: 3f, available: 0));
        }

        [Test]
        public void EnemyBudget_ClosesTheGapToTheTarget()
        {
            // shape 4 + 3 enemies x 3.0 = 13, the closest reachable to 13.
            Assert.AreEqual(3, LevelDifficulty.EnemyBudget(target: 13f, shapeScore: 4f, available: 5));
        }

        // ---------- Applying the budget to a level ----------

        [Test]
        public void ApplyEnemyBudget_EnablesExactlyTheBudgetedEnemies()
        {
            var level = new GameObject("Level");
            var enemies = new GameObject("Enemies");
            enemies.transform.SetParent(level.transform);
            for (int i = 0; i < 5; i++)
                new GameObject($"Enemy_{i}").transform.SetParent(enemies.transform);

            try
            {
                Assert.AreEqual(5, LevelDifficulty.AvailableEnemies(level.transform));

                LevelDifficulty.ApplyEnemyBudget(level.transform, 2);
                Assert.AreEqual(2, ActiveEnemies(enemies.transform));

                // Must be able to go back up as well as down — the same level is re-served with a
                // different budget on a later cycle.
                LevelDifficulty.ApplyEnemyBudget(level.transform, 4);
                Assert.AreEqual(4, ActiveEnemies(enemies.transform));

                LevelDifficulty.ApplyEnemyBudget(level.transform, 0);
                Assert.AreEqual(0, ActiveEnemies(enemies.transform));
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void ApplyEnemyBudget_OnALevelWithNoEnemiesGroup_IsANoOp()
        {
            var level = new GameObject("Level");
            try
            {
                Assert.AreEqual(0, LevelDifficulty.AvailableEnemies(level.transform));
                Assert.DoesNotThrow(() => LevelDifficulty.ApplyEnemyBudget(level.transform, 3));
                Assert.DoesNotThrow(() => LevelDifficulty.ApplyEnemyBudget(null, 3));
                Assert.AreEqual(0f, LevelDifficulty.ShapeScore(null), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(level);
            }
        }

        [Test]
        public void LastLevelEnemies_DefaultsToUnknownRatherThanZero()
        {
            // -1 has to mean "no idea, leave the level as authored". Defaulting to 0 would strip
            // every enemy from a fresh install's first resumed level instead.
            PlayerPrefs.DeleteKey("LastLevelEnemies");
            Assert.AreEqual(-1, PlayerProgress.LastLevelEnemies);

            int restore = PlayerProgress.LastLevelEnemies;
            PlayerProgress.LastLevelEnemies = 2;
            Assert.AreEqual(2, PlayerProgress.LastLevelEnemies);
            PlayerProgress.LastLevelEnemies = restore;
        }

        private static int ActiveEnemies(Transform enemies)
        {
            int n = 0;
            for (int i = 0; i < enemies.childCount; i++)
                if (enemies.GetChild(i).gameObject.activeSelf) n++;
            return n;
        }
    }
}
