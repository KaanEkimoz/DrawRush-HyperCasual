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
    ///   • Tank from the 12th level: slow, big, cannot be pushed (the pet answers the sprinters,
    ///     the shield and freeze answer the tank).
    /// Each type's first level fields it alone. After two losses in a row on the same level the
    /// enemies quietly slow down a little (silent DDA, never announced).
    /// </summary>
    public sealed class EnemyTierDirector : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerHealth health;

        [Header("Introduction (campaign level number, 1-based)")]
        [SerializeField] private int sprinterFrom = 6;
        [SerializeField] private int tankFrom = 12;
        [SerializeField] private int doubleFrom = 20;

        [Header("Sprinter")]
        [SerializeField] private float sprinterSpeed = 1.5f;
        [SerializeField] private float sprinterScale = 0.8f;
        [SerializeField] private Color sprinterColor = new(1f, 0.85f, 0.1f, 1f);

        [Header("Tank")]
        [SerializeField] private float tankSpeed = 0.7f;
        [SerializeField] private float tankScale = 1.35f;
        [SerializeField] private Color tankColor = new(0.35f, 0.33f, 0.45f, 1f);

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

            Plan(PlayerProgress.LevelsPlayed, out int sprinters, out int tanks, out bool intro);
            if (intro)
                for (int i = 1; i < enemies.Count; i++) enemies[i].gameObject.SetActive(false);   // the new type, alone

            int k = 0;
            float help = _fails >= failsBeforeHelp ? helpSpeed : 1f;
            for (; k < enemies.Count && tanks > 0; k++, tanks--)
                if (enemies[k].isActiveAndEnabled) enemies[k].ApplyVariant(tankSpeed * help, tankScale, true, tankColor);
            for (; k < enemies.Count && sprinters > 0; k++, sprinters--)
                if (enemies[k].isActiveAndEnabled) enemies[k].ApplyVariant(sprinterSpeed * help, sprinterScale, false, sprinterColor);
            if (help < 1f)
                for (; k < enemies.Count; k++)
                    if (enemies[k].isActiveAndEnabled) enemies[k].ApplyVariant(help, 1f, false, null);
        }

        /// <summary>How many of each variant a campaign level fields, and whether it introduces one.</summary>
        public void Plan(int levelNumber, out int sprinters, out int tanks, out bool intro)
        {
            sprinters = levelNumber >= sprinterFrom ? (levelNumber >= doubleFrom ? 2 : 1) : 0;
            tanks = levelNumber >= tankFrom ? (levelNumber >= doubleFrom + 6 ? 2 : 1) : 0;
            intro = levelNumber == sprinterFrom || levelNumber == tankFrom;
            if (levelNumber == tankFrom) sprinters = 0;   // the tank's debut is the tank alone
        }

        private static int CurrentIndex()
        {
            var lm = FindFirstObjectByType<LevelManager>();
            return lm != null ? lm.CurrentIndex : -1;
        }
    }
}
