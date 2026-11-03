using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Player;
using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Brings in new enemy types over the campaign, one at a time (research: introduce a threat
    /// alone, in an easier level, then mix it with the known ones):
    ///   • Sprinter from the 6th level: fast, small, bright;
    ///   • Tank from the 12th level: slow, big, cannot be pushed;
    ///   • Eater from the 18th: wipes paint that is not kept for good yet (draw, then move on);
    ///   • Dasher from the 25th: flashes, then lunges (read the telegraph, sidestep).
    /// Each type's first level fields it alone. Once the campaign has been played through, every
    /// further lap is a harder tier: one more variant of each kind and slightly faster enemies.
    /// Every tenth level is a boss level: the Giant Eraser joins the field (a level whose budget
    /// gave it no enemies gets one for the boss).
    /// After two losses in a row on the same level the enemies quietly slow down a little
    /// (silent DDA, never announced).
    /// </summary>
    public sealed class EnemyTierDirector : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerHealth health;

        [Header("Introduction (campaign level number, 1-based)")]
        [SerializeField] private int sprinterFrom = 6;
        [SerializeField] private int tankFrom = 12;
        [SerializeField] private int eaterFrom = 18;
        [SerializeField] private int dasherFrom = 25;
        [SerializeField] private int flankerFrom = 9;
        [SerializeField] private int orbiterFrom = 15;
        [SerializeField] private int doubleFrom = 20;
        [Tooltip("Levels in one lap of the campaign (the tutorial excluded). Past it, each lap is a harder tier.")]
        [SerializeField] private int campaignLength = 34;
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

        [Header("Boss: Giant Eraser")]
        [Tooltip("Every Nth campaign level is a boss level.")]
        [SerializeField] private int bossEvery = 10;
        [SerializeField] private float bossSpeed = 0.675f;
        [SerializeField] private float bossScale = 1.8f;

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
            EnemyFollow boss = IsBossLevel(number) ? FieldBoss(network, enemies) : null;
            if (enemies.Count == 0) return;
            float speed = (_fails >= failsBeforeHelp ? helpSpeed : 1f) * TierSpeed(number);
            if (boss != null)
            {
                // A boss fight is a duel: the Giant Eraser is the only enemy in its level.
                foreach (EnemyFollow e in enemies)
                    if (e != boss) e.gameObject.SetActive(false);
                boss.ApplyVariant(EnemyVariant.Boss, bossSpeed * speed, bossScale, true, null);
                var hud = FindFirstObjectByType<DrawRush.Boosters.BoosterHud>();
                if (hud != null) hud.Toast("GIANT ERASER!", "Keep moving, it wipes fresh paint", new Color(0.9f, 0.25f, 0.4f));
                return;
            }

            Plan(number, out int sprinters, out int tanks, out int eaters, out int dashers, out int flankers, out int orbiters, out bool intro);
            if (intro)
                for (int i = 1; i < enemies.Count; i++) enemies[i].gameObject.SetActive(false);   // the new type, alone

            int k = 0;
            k = Dress(enemies, k, tanks, EnemyVariant.Tank, tankSpeed * speed, tankScale, true, tankColor);
            k = Dress(enemies, k, dashers, EnemyVariant.Dasher, speed, 1f, false, dasherColor);
            k = Dress(enemies, k, eaters, EnemyVariant.Eater, speed, 1f, false, eaterColor);
            k = Dress(enemies, k, flankers, EnemyVariant.Flanker, flankerSpeed * speed, 1f, false, null);
            k = Dress(enemies, k, orbiters, EnemyVariant.Orbiter, orbiterSpeed * speed, orbiterScale, false, null);
            k = Dress(enemies, k, sprinters, EnemyVariant.Sprinter, sprinterSpeed * speed, sprinterScale, false, sprinterColor);
            if (!Mathf.Approximately(speed, 1f))
                for (; k < enemies.Count; k++)
                    if (enemies[k].isActiveAndEnabled) enemies[k].ApplyVariant(EnemyVariant.Normal, speed, 1f, false, null);
        }

        private static int Dress(List<EnemyFollow> enemies, int k, int count, EnemyVariant v, float speed, float scale, bool heavy, Color? tint)
        {
            for (; k < enemies.Count && count > 0; k++)
            {
                if (!enemies[k].isActiveAndEnabled) continue;
                enemies[k].ApplyVariant(v, speed, scale, heavy, tint);
                count--;
            }
            return k;
        }

        /// <summary>True for a boss level (0-based count of levels played).</summary>
        public bool IsBossLevel(int levelNumber) => bossEvery > 0 && levelNumber > 0 && levelNumber % bossEvery == 0;

        // The level's one enemy that carries the boss visual, switched on if the budget left it off.
        private static EnemyFollow FieldBoss(EdgeNetwork network, List<EnemyFollow> enemies)
        {
            // The root holds every level: climb only to this level, or the boss comes from another.
            Transform level = network.transform;
            while (level.parent != null && level.parent.parent != null) level = level.parent;
            foreach (EnemyFollow e in level.GetComponentsInChildren<EnemyFollow>(true))
            {
                if (!e.CanBeBoss) continue;
                if (!e.gameObject.activeSelf) e.gameObject.SetActive(true);
                if (!enemies.Contains(e)) enemies.Add(e);
                return e;
            }
            return null;
        }

        /// <summary>Hard-tier lap for a campaign level number: 0 on the first play-through.</summary>
        public int Lap(int levelNumber) => campaignLength > 0 ? Mathf.Max(0, (levelNumber - 1) / campaignLength) : 0;

        private float TierSpeed(int levelNumber) => Mathf.Min(maxTierSpeed, 1f + tierSpeedStep * Lap(levelNumber));

        /// <summary>How many of each variant a campaign level fields, and whether it introduces one.</summary>
        public void Plan(int levelNumber, out int sprinters, out int tanks, out int eaters, out int dashers, out bool intro)
            => Plan(levelNumber, out sprinters, out tanks, out eaters, out dashers, out _, out _, out intro);

        /// <summary>The full plan, with the flankers and orbiters.</summary>
        public void Plan(int levelNumber, out int sprinters, out int tanks, out int eaters, out int dashers,
                         out int flankers, out int orbiters, out bool intro)
        {
            int lap = Lap(levelNumber);
            int n = lap > 0 ? campaignLength : levelNumber;   // later laps keep the full roster unlocked
            sprinters = n >= sprinterFrom ? (n >= doubleFrom ? 2 : 1) : 0;
            tanks = n >= tankFrom ? (n >= doubleFrom + 6 ? 2 : 1) : 0;
            eaters = n >= eaterFrom ? 1 : 0;
            dashers = n >= dasherFrom ? 1 : 0;
            flankers = n >= flankerFrom ? (n >= doubleFrom + 8 ? 2 : 1) : 0;
            orbiters = n >= orbiterFrom ? 1 : 0;
            if (lap > 0) { sprinters++; tanks++; eaters++; dashers++; flankers++; orbiters++; }   // the hard tier: one more of each

            intro = lap == 0 && (levelNumber == sprinterFrom || levelNumber == tankFrom || levelNumber == eaterFrom
                                 || levelNumber == dasherFrom || levelNumber == flankerFrom || levelNumber == orbiterFrom);
            if (!intro) return;
            // An introduction fields only the new type.
            sprinters = levelNumber == sprinterFrom ? 1 : 0;
            tanks = levelNumber == tankFrom ? 1 : 0;
            eaters = levelNumber == eaterFrom ? 1 : 0;
            dashers = levelNumber == dasherFrom ? 1 : 0;
            flankers = levelNumber == flankerFrom ? 1 : 0;
            orbiters = levelNumber == orbiterFrom ? 1 : 0;
        }

        private static int CurrentIndex()
        {
            var lm = FindFirstObjectByType<LevelManager>();
            return lm != null ? lm.CurrentIndex : -1;
        }
    }
}
