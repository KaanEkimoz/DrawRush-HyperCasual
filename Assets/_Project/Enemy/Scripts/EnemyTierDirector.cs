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
            if (enemies.Count == 0) return;

            int number = PlayerProgress.LevelsPlayed;
            Plan(number, out int sprinters, out int tanks, out int eaters, out int dashers, out bool intro);
            if (intro)
                for (int i = 1; i < enemies.Count; i++) enemies[i].gameObject.SetActive(false);   // the new type, alone

            float speed = (_fails >= failsBeforeHelp ? helpSpeed : 1f) * TierSpeed(number);
            int k = 0;
            k = Dress(enemies, k, tanks, EnemyVariant.Tank, tankSpeed * speed, tankScale, true, tankColor);
            k = Dress(enemies, k, dashers, EnemyVariant.Dasher, speed, 1f, false, dasherColor);
            k = Dress(enemies, k, eaters, EnemyVariant.Eater, speed, 1f, false, eaterColor);
            k = Dress(enemies, k, sprinters, EnemyVariant.Sprinter, sprinterSpeed * speed, sprinterScale, false, sprinterColor);
            if (!Mathf.Approximately(speed, 1f))
                for (; k < enemies.Count; k++)
                    if (enemies[k].isActiveAndEnabled) enemies[k].ApplyVariant(EnemyVariant.Normal, speed, 1f, false, null);
        }

        private static int Dress(List<EnemyFollow> enemies, int k, int count, EnemyVariant v, float speed, float scale, bool heavy, Color tint)
        {
            for (; k < enemies.Count && count > 0; k++)
            {
                if (!enemies[k].isActiveAndEnabled) continue;
                enemies[k].ApplyVariant(v, speed, scale, heavy, tint);
                count--;
            }
            return k;
        }

        /// <summary>Hard-tier lap for a campaign level number: 0 on the first play-through.</summary>
        public int Lap(int levelNumber) => campaignLength > 0 ? Mathf.Max(0, (levelNumber - 1) / campaignLength) : 0;

        private float TierSpeed(int levelNumber) => Mathf.Min(maxTierSpeed, 1f + tierSpeedStep * Lap(levelNumber));

        /// <summary>How many of each variant a campaign level fields, and whether it introduces one.</summary>
        public void Plan(int levelNumber, out int sprinters, out int tanks, out int eaters, out int dashers, out bool intro)
        {
            int lap = Lap(levelNumber);
            int n = lap > 0 ? campaignLength : levelNumber;   // later laps keep the full roster unlocked
            sprinters = n >= sprinterFrom ? (n >= doubleFrom ? 2 : 1) : 0;
            tanks = n >= tankFrom ? (n >= doubleFrom + 6 ? 2 : 1) : 0;
            eaters = n >= eaterFrom ? 1 : 0;
            dashers = n >= dasherFrom ? 1 : 0;
            if (lap > 0) { sprinters++; tanks++; eaters++; dashers++; }   // the hard tier: one more of each

            intro = lap == 0 && (levelNumber == sprinterFrom || levelNumber == tankFrom || levelNumber == eaterFrom || levelNumber == dasherFrom);
            if (!intro) return;
            // An introduction fields only the new type.
            sprinters = levelNumber == sprinterFrom ? 1 : 0;
            tanks = levelNumber == tankFrom ? 1 : 0;
            eaters = levelNumber == eaterFrom ? 1 : 0;
            dashers = levelNumber == dasherFrom ? 1 : 0;
        }

        private static int CurrentIndex()
        {
            var lm = FindFirstObjectByType<LevelManager>();
            return lm != null ? lm.CurrentIndex : -1;
        }
    }
}
