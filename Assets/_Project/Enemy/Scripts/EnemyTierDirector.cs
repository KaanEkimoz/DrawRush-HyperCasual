using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Player;
using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Dresses each level's enemies for the campaign. WHICH kind comes WHEN is not decided here but
    /// in <see cref="Campaign"/>, the one rule book for enemies, traps and desk accidents: one new
    /// kind at a time on an easy level, alone, then rotated back in so every kind keeps coming.
    /// Every tenth level is a boss duel: the Giant Eraser (10, 30, 50...) and the Giant Stapler
    /// (20, 40...). Once the campaign has been played through, every further lap is a harder tier
    /// with slightly faster enemies. After two losses in a row on the same level the enemies quietly
    /// slow down a little (silent DDA, never announced).
    /// </summary>
    public sealed class EnemyTierDirector : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerHealth health;
        [Tooltip("Levels in one lap of the campaign (the tutorial excluded), when no LevelManager is " +
                 "around to count them. Past a lap, each lap is a harder tier.")]
        [SerializeField] private int campaignLength = 54;
        [Tooltip("Speed added per hard-tier lap, capped by maxTierSpeed.")]
        [SerializeField] private float tierSpeedStep = 0.08f;
        [SerializeField] private float maxTierSpeed = 1.25f;

        [Header("Sprinter")]
        [SerializeField] private float sprinterSpeed = 1.5f;
        [SerializeField] private float sprinterScale = 0.8f;
        [SerializeField] private Color sprinterColor = new(1f, 0.85f, 0.1f, 1f);

        [Header("Tank")]
        [SerializeField] private float tankSpeed = 0.7f;
        [SerializeField] private float tankScale = 1.35f;
        [SerializeField] private Color tankColor = new(0.35f, 0.33f, 0.45f, 1f);

        [Header("Eater")]
        [SerializeField] private Color eaterColor = new(0.35f, 0.85f, 0.25f, 1f);

        [Header("Dasher")]
        [SerializeField] private Color dasherColor = new(1f, 0.3f, 0.55f, 1f);

        [Header("Flanker / Orbiter")]
        [SerializeField] private float flankerSpeed = 1.1f;
        [SerializeField] private float orbiterSpeed = 1.25f;
        [SerializeField] private float orbiterScale = 0.8f;
        [SerializeField] private float glueSpeed = 0.92f;

        [Header("Boss: Giant Eraser")]
        [SerializeField] private float bossSpeed = 0.675f;
        [SerializeField] private float bossScale = 1.8f;

        [Header("Boss: Giant Stapler (every other boss level)")]
        [SerializeField] private float staplerBossSpeed = 0.75f;
        [SerializeField] private float staplerBossScale = 2.1f;

        [Header("Silent help")]
        [SerializeField] private int failsBeforeHelp = 2;
        [SerializeField] private float helpSpeed = 0.9f;

        private int _failLevel = -1, _fails;

        private void OnEnable()
        {
            if (painter == null) painter = FindFirstObjectByType<FreeDrawPainter>();
            if (painter != null) painter.LevelBound += OnLevelBound;
            if (health != null) health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (painter != null) painter.LevelBound -= OnLevelBound;
            if (health != null) health.Died -= OnDied;
        }

        private void OnDied()
        {
            int level = CurrentIndex();
            if (level == _failLevel) _fails++;
            else { _failLevel = level; _fails = 1; }
        }

        private void OnLevelBound(EdgeNetwork network)
        {
            int level = CurrentIndex();
            if (level != _failLevel) { _failLevel = level; _fails = 0; }

            var enemies = new List<EnemyFollow>();
            foreach (EnemyFollow e in network.transform.root.GetComponentsInChildren<EnemyFollow>(false))
                if (e.isActiveAndEnabled) enemies.Add(e);
            foreach (EnemyFollow e in enemies) e.ClearVariant();

            int number = PlayerProgress.LevelsPlayed;
            bool stapler = IsStaplerBossLevel(number);
            EnemyFollow boss = IsBossLevel(number) ? FieldBoss(network, enemies, needsEraserLook: !stapler) : null;
            Novelty debut = Campaign.DebutAt(number);
            bool intro = Campaign.IsEnemy(debut);
            // A debut must actually happen: a level the curve left empty gets one enemy for it.
            if (intro && enemies.Count == 0) FieldOne(network, enemies);
            if (enemies.Count == 0) return;
            float speed = (_fails >= failsBeforeHelp ? helpSpeed : 1f) * TierSpeed(number);
            var hud = FindFirstObjectByType<DrawRush.Boosters.BoosterHud>();
            if (boss != null)
            {
                // A boss fight is a duel: the boss is the only enemy in its level.
                foreach (EnemyFollow e in enemies)
                    if (e != boss) e.gameObject.SetActive(false);
                if (stapler)
                {
                    boss.ApplyVariant(EnemyVariant.BossStapler, staplerBossSpeed * speed, staplerBossScale, true, null);
                    if (hud != null) hud.Toast("GIANT STAPLER!", "It leaps onto the red ring: get out!", new Color(0.85f, 0.2f, 0.2f));
                }
                else
                {
                    boss.ApplyVariant(EnemyVariant.Boss, bossSpeed * speed, bossScale, true, null);
                    if (hud != null) hud.Toast("GIANT ERASER!", "Keep moving, it wipes fresh paint", new Color(0.9f, 0.25f, 0.4f));
                }
                return;
            }

            if (intro)
            {
                for (int i = 1; i < enemies.Count; i++) enemies[i].gameObject.SetActive(false);   // the new type, alone
                enemies.RemoveRange(1, enemies.Count - 1);
                if (hud != null) IntroToast(hud, debut);
            }

            // Each slot wears the kind the campaign rotates in for it.
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].isActiveAndEnabled) Dress(enemies[i], Campaign.EnemyKind(number, i), speed);
        }

        private void Dress(EnemyFollow e, Novelty kind, float speed)
        {
            switch (kind)
            {
                case Novelty.Scissors: e.ApplyVariant(EnemyVariant.Sprinter, sprinterSpeed * speed, sprinterScale, false, sprinterColor); break;
                case Novelty.HolePunch: e.ApplyVariant(EnemyVariant.Tank, tankSpeed * speed, tankScale, true, tankColor); break;
                case Novelty.Eraser: e.ApplyVariant(EnemyVariant.Flanker, flankerSpeed * speed, 1f, false, null); break;
                case Novelty.Sharpener: e.ApplyVariant(EnemyVariant.Eater, speed, 1f, false, eaterColor); break;
                case Novelty.Putty: e.ApplyVariant(EnemyVariant.Orbiter, orbiterSpeed * speed, orbiterScale, false, null); break;
                case Novelty.Glue: e.ApplyVariant(EnemyVariant.Glue, glueSpeed * speed, 1f, false, null); break;
                case Novelty.Pushpin: e.ApplyVariant(EnemyVariant.Dasher, speed, 1f, false, dasherColor); break;
                default:
                    if (!Mathf.Approximately(speed, 1f)) e.ApplyVariant(EnemyVariant.Normal, speed, 1f, false, null);
                    break;
            }
        }

        /// <summary>True for a boss level (0-based count of levels played).</summary>
        public bool IsBossLevel(int levelNumber) => Campaign.IsBoss(levelNumber);

        /// <summary>Boss levels alternate: the Giant Eraser on the 10th, 30th..., the Giant Stapler on the 20th, 40th...</summary>
        public bool IsStaplerBossLevel(int levelNumber) => Campaign.IsStaplerBoss(levelNumber);

        // The level's one enemy that can play the boss, switched on if the budget left it off. The
        // eraser needs its own visual; the stapler boss is any enemy (the stapler is the default look).
        private static EnemyFollow FieldBoss(EdgeNetwork network, List<EnemyFollow> enemies, bool needsEraserLook)
        {
            foreach (EnemyFollow e in LevelEnemies(network))
            {
                if (needsEraserLook && !e.CanBeBoss) continue;
                if (!e.gameObject.activeSelf) e.gameObject.SetActive(true);
                if (!enemies.Contains(e)) enemies.Add(e);
                return e;
            }
            return null;
        }

        private static void FieldOne(EdgeNetwork network, List<EnemyFollow> enemies)
        {
            foreach (EnemyFollow e in LevelEnemies(network))
            {
                e.gameObject.SetActive(true);
                if (!e.isActiveAndEnabled) continue;   // its group is off: try another
                enemies.Add(e);
                return;
            }
        }

        // The root holds every level: climb only to this level, or the enemy comes from another.
        private static EnemyFollow[] LevelEnemies(EdgeNetwork network)
        {
            Transform level = network.transform;
            while (level.parent != null && level.parent.parent != null) level = level.parent;
            return level.GetComponentsInChildren<EnemyFollow>(true);
        }

        private static void IntroToast(DrawRush.Boosters.BoosterHud hud, Novelty kind)
        {
            var c = new Color(0.25f, 0.45f, 0.95f);
            switch (kind)
            {
                case Novelty.Stapler: hud.Toast("STAPLER!", "It chases you. Keep drawing, keep moving", c); break;
                case Novelty.Scissors: hud.Toast("NEW: SCISSORS", "Small and fast", c); break;
                case Novelty.HolePunch: hud.Toast("NEW: HOLE PUNCH", "Slow, but nothing pushes it", c); break;
                case Novelty.Eraser: hud.Toast("NEW: ERASER", "It runs to where you're going", c); break;
                case Novelty.Sharpener: hud.Toast("NEW: SHARPENER", "It sucks you in: run away!", c); break;
                case Novelty.Putty: hud.Toast("NEW: PUTTY", "Leave the red ring before the chunk lands", c); break;
                case Novelty.Glue: hud.Toast("NEW: GLUE", "Its puddles slow you down", c); break;
                case Novelty.Pushpin: hud.Toast("NEW: PUSHPIN", "It flashes, then leaps: sidestep", c); break;
            }
        }

        /// <summary>Hard-tier lap for a campaign level number: 0 on the first play-through.</summary>
        public int Lap(int levelNumber)
        {
            int length = CampaignLength();
            return length > 0 ? Mathf.Max(0, (levelNumber - 1) / length) : 0;
        }

        // One lap = every drawable shape once (the tutorial excluded).
        private int CampaignLength()
        {
            var lm = FindFirstObjectByType<LevelManager>();
            return lm != null && lm.LevelCount > 1 ? lm.LevelCount - 1 : campaignLength;
        }

        private float TierSpeed(int levelNumber) => Mathf.Min(maxTierSpeed, 1f + tierSpeedStep * Lap(levelNumber));

        private static int CurrentIndex()
        {
            var lm = FindFirstObjectByType<LevelManager>();
            return lm != null ? lm.CurrentIndex : -1;
        }
    }
}
