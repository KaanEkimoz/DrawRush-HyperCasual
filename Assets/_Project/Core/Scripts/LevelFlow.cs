using System.Collections.Generic;
using UnityEngine;
using DrawRush.Ads;

namespace DrawRush.Core
{
    /// <summary>
    /// Owns level navigation. In the mega-scene architecture there is no per-level scene reload:
    /// navigation delegates to <see cref="LevelManager"/>, which enables one level group at a time
    /// and resets per-level state.
    ///
    /// Which level comes next is not "the next index" and not a coin flip — it is chosen to land on
    /// a difficulty target that follows a sawtooth (climb, drop, climb from higher). Two pieces do
    /// that: a <see cref="LevelBag"/> picks an unseen SHAPE near the target so nothing repeats until
    /// every level has been seen, and <see cref="LevelDifficulty"/> then dials that level's ENEMY
    /// count to close the remaining gap — including down to zero, which is how a late, complex shape
    /// can still serve as a breather.
    /// </summary>
    public sealed class LevelFlow : MonoBehaviour
    {
        [Tooltip("PlayerPrefs key used to persist the displayed level number.")]
        [SerializeField] private string playerPrefsKey = "Level";

        [Tooltip("Switcher that enables level groups in the mega-scene.")]
        [SerializeField] private LevelManager levelManager;

        [Tooltip("Optional. Shows an interstitial between levels on its own cadence. Left unset (or " +
                 "with no ad unit) the game runs ad-free — the hook no-ops.")]
        [SerializeField] private AdManager ads;

        [Header("Difficulty Curve")]
        [Tooltip("Difficulty target for the very first level after the tutorial. Low enough that " +
                 "the bag picks the simplest shape and the enemy budget works out to zero.")]
        [SerializeField] private float minTarget = 3f;

        [Tooltip("Difficulty target at the very top of the curve. Keep this inside what the levels " +
                 "can actually be dialled up to (shape + every authored enemy) — set it higher and " +
                 "the peaks just sit permanently under target. 34 levels: 9 reach 20, only 4 reach 24.")]
        [SerializeField] private float maxTarget = 21f;

        [Tooltip("Levels per sawtooth tooth: difficulty climbs for this many, then drops to minTarget.")]
        [SerializeField] private int sawPeriod = 5;

        [Tooltip("Levels it takes for the teeth to grow from a beginner's peak to the full maxTarget.")]
        [SerializeField] private int rampLevels = 30;

        [Tooltip("How many of the closest-to-target levels the bag chooses between. 1 = a fixed " +
                 "campaign: every player gets the same order (Kaan: levels must feel like 1→2→3, " +
                 "not a shuffle). Higher = the curve is still honoured but the order varies.")]
        [SerializeField] private int candidatePool = 1;

        private readonly LevelBag _bag = new();

        public int CurrentLevel => PlayerPrefs.GetInt(playerPrefsKey, 1);

        private void Awake()
        {
            // LevelFlow may be added at runtime by GameManager, so the Inspector
            // wiring can be absent — resolve the scene's LevelManager as a fallback.
            if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
            if (ads == null) ads = FindFirstObjectByType<AdManager>();
            _bag.CandidatePool = Mathf.Max(1, candidatePool);
            _bag.Deserialize(PlayerProgress.LevelBag);
            // Shapes added in an update join the saved bag, so they are not held back a whole cycle.
            if (levelManager != null) _bag.AddNewLevels(levelManager.LevelCount, levelManager.TutorialIndex);
        }

        public void StartTheGame()
        {
            MenuPause.SetGameTimeScale(1f);
        }

        public void RestartLevel()
        {
            if (levelManager == null) return;
            MenuPause.SetGameTimeScale(1f);   // in-scene switch: no scene reload to un-pause for us
            // Deliberately does NOT draw or advance the curve: a retry is the same level at the
            // same difficulty, otherwise dying would quietly reshuffle the player's run.
            PlayerProgress.WonLevelPendingAdvance = -1;   // replaying a won level is a choice, not a quit
            levelManager.ActivateLevel(levelManager.CurrentIndex);
        }

        /// <summary>Serve the next level: pick a target off the sawtooth, draw the nearest unseen
        /// shape from the bag, then set that level's enemy count to hit the target.</summary>
        public void NextLevel()
        {
            if (levelManager == null || levelManager.LevelCount == 0) return;
            MenuPause.SetGameTimeScale(1f);

            int played = PlayerProgress.LevelsPlayed;
            // One win/advance: let the ad layer decide whether to show (it caps by win-count AND a
            // minimum time gap). Shown here it overlays the win panel while the next level loads
            // underneath, so closing it reveals a ready level. No-ops unless both gates pass and an
            // ad is loaded.
            if (ads != null) ads.MaybeShowInterstitial();
            float target = LevelDifficulty.SawtoothTarget(played, sawPeriod, minTarget, maxTarget, rampLevels);

            int next = _bag.Draw(target, i => AttainableScore(i, target), DrawableLevels());
            if (next < 0) return;

            // Tune enemies BEFORE activation, so the level is never briefly live with the wrong
            // count (and so no enemy gets an Awake it shouldn't have had). Same budget the draw
            // ranked on, so the level delivers the difficulty it was chosen for.
            Transform level = levelManager.GetLevel(next);
            int enemies = EnemiesFor(played + 1, target, level);
            LevelDifficulty.ApplyEnemyBudget(level, enemies);

            PlayerProgress.WonLevelPendingAdvance = -1;
            PlayerProgress.LevelsPlayed = played + 1;
            PlayerProgress.LevelBag = _bag.Serialize();
            // Save the count too, not just the index: the enemy budget is a per-playthrough choice
            // and would be lost on relaunch, bringing the level back fully armed.
            PlayerProgress.LastLevelEnemies = enemies;
            PlayerPrefs.SetInt(playerPrefsKey, CurrentLevel + 1);

            levelManager.ActivateLevel(next);
        }

        /// <summary>Apply the enemy budget the difficulty curve wants for the level at
        /// <paramref name="index"/> at the CURRENT played count, WITHOUT advancing the curve. Used to
        /// budget the level the game boots directly into (not reached through <see cref="NextLevel"/>),
        /// which otherwise came up with every authored enemy switched on — so the first level after
        /// the tutorial works out to zero enemies, the gentle start it was always meant to be.</summary>
        public void BudgetLevelForCurve(int index)
        {
            if (levelManager == null) return;
            Transform level = levelManager.GetLevel(index);
            if (level == null) return;
            // The level on screen is number LevelsPlayed; NextLevel aimed it at the curve one step
            // back (the count before it was bumped), so the same target is used here.
            int number = Mathf.Max(1, PlayerProgress.LevelsPlayed);
            float target = LevelDifficulty.SawtoothTarget(number - 1, sawPeriod, minTarget, maxTarget, rampLevels);
            int enemies = EnemiesFor(number, target, level);
            LevelDifficulty.ApplyEnemyBudget(level, enemies);
            PlayerProgress.LastLevelEnemies = enemies;   // persist so a relaunch restores this count
        }

        /// <summary>Kept because scene/prefab UI still references it by name. It used to pick a
        /// uniformly random level, which is what served a 3-enemy star straight after the tutorial;
        /// it now goes through the same curve as everything else.</summary>
        public void LoadRandomLevel() => NextLevel();

        // What this level would actually be worth if drawn right now — shape plus however many
        // enemies it would field to chase the target. Ranking on the bare shape score instead would
        // compare a shape-only number against a target that includes enemies, so every high target
        // would drag out the most complex shape left and enemies would only ever be a leftover.
        // Ranking on what's attainable lets a middling shape with enemies behind it answer a high
        // target, which is the whole point of enemies being the dial.
        private float AttainableScore(int index, float target)
        {
            int number = PlayerProgress.LevelsPlayed + 1;
            return RankScore(number, target, LevelDifficulty.ShapeScore(levelManager.GetLevel(index)),
                LevelDifficulty.AvailableEnemies(levelManager.GetLevel(index)));
        }

        /// <summary>What a level is worth when served as campaign level <paramref name="number"/>:
        /// its shape, the traps and accidents the campaign puts on that level, and the enemies the
        /// budget adds on top.</summary>
        public static float Attainable(int number, float target, float shape, int available) =>
            shape + Campaign.FixedCost(number) + Enemies(number, target, shape, available) * LevelDifficulty.EnemyWeight;

        /// <summary>Enemies for campaign level <paramref name="number"/>: whatever closes the gap
        /// to the target once the shape, traps and accidents are counted, within the campaign's cap
        /// for that level (none on breathers and under trap / accident debuts, one for a debut).</summary>
        public static int Enemies(int number, float target, float shape, int available) =>
            Mathf.Min(Mathf.Min(Campaign.EnemyCap(number), available), Mathf.Max(Campaign.EnemyFloor(number),
                LevelDifficulty.EnemyBudget(target - Campaign.FixedCost(number), shape, available)));

        /// <summary>The bag's ranking score: what the level is worth, with debut levels steered to
        /// simple shapes (a new thing is learnt on an easy page).</summary>
        public static float RankScore(int number, float target, float shape, int available)
        {
            float score = Attainable(number, target, shape, available);
            if (Campaign.DebutAt(number) != Novelty.None) score += Mathf.Max(0f, shape - Campaign.DebutShapeMax) * 2f;
            return score;
        }

        private static int EnemiesFor(int number, float target, Transform level) =>
            Enemies(number, target, LevelDifficulty.ShapeScore(level), LevelDifficulty.AvailableEnemies(level));

        // Everything the bag may hand out: all level groups except the tutorial, which is gated by
        // its own completed-flag and must never come round again as "the next level".
        private IEnumerable<int> DrawableLevels()
        {
            for (int i = 0; i < levelManager.LevelCount; i++)
                if (i != levelManager.TutorialIndex) yield return i;
        }
    }
}
