#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.IO;
using System.Text;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Enemy;
using DrawRush.Player;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Development-only playtest log: one CSV row per level attempt (win, loss or abandoned), so
    /// difficulty, booster and coin numbers can be tuned from real play instead of guesses. The
    /// file lives at Application.persistentDataPath/playtest_log.csv (on a device:
    /// <c>adb pull /sdcard/Android/data/com.ekimozgames.drawrush/files/playtest_log.csv</c>).
    /// Compiled out of release builds.
    /// </summary>
    public sealed class PlaytestLog : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerBoosts boosts;
        [SerializeField] private PlayerHealth health;

        private const string Header = "time,campaign_level,level,enemies,sprinters,tanks,eaters,dashers,result,seconds,coverage,hits,paint_lost,boosters,stars,coins";

        private string _path;
        private bool _open;
        private string _level;
        private int _campaign, _enemies, _sprinters, _tanks, _eaters, _dashers, _hits, _lost, _boosters;
        private float _start;

        private void Awake()
        {
            _path = Path.Combine(Application.persistentDataPath, "playtest_log.csv");
            if (!File.Exists(_path)) File.WriteAllText(_path, Header + "\n");
        }

        private void OnEnable()
        {
            if (painter != null) { painter.LevelBound += OnLevelBound; painter.PaintLost += OnPaintLost; }
            if (combat != null) combat.Damaged += OnHit;
            if (boosts != null) boosts.Granted += OnBooster;
            if (health != null) health.Died += OnDied;
            var state = GameServices.State;
            if (state != null) state.GameWonChanged += OnWon;
        }

        private void OnDisable()
        {
            if (painter != null) { painter.LevelBound -= OnLevelBound; painter.PaintLost -= OnPaintLost; }
            if (combat != null) combat.Damaged -= OnHit;
            if (boosts != null) boosts.Granted -= OnBooster;
            if (health != null) health.Died -= OnDied;
            var state = GameServices.State;
            if (state != null) state.GameWonChanged -= OnWon;
        }

        private void OnLevelBound(EdgeNetwork network)
        {
            if (_open) Write("abandoned", 0);   // restarted or skipped without an outcome
            _open = true;
            _level = network != null ? LevelName(network) : "?";
            _campaign = PlayerProgress.LevelsPlayed;
            _hits = _lost = _boosters = 0;
            _start = Time.time;
            // Count once the tier director has dressed the enemies (it listens to the same event).
            Invoke(nameof(CountEnemies), 0.1f);
        }

        private void CountEnemies()
        {
            _enemies = _sprinters = _tanks = _eaters = _dashers = 0;
            foreach (EnemyFollow e in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                _enemies++;
                switch (e.Variant)
                {
                    case EnemyVariant.Sprinter: _sprinters++; break;
                    case EnemyVariant.Tank: _tanks++; break;
                    case EnemyVariant.Eater: _eaters++; break;
                    case EnemyVariant.Dasher: _dashers++; break;
                }
            }
        }

        private void OnHit() => _hits++;
        private void OnPaintLost() => _lost++;
        private void OnBooster(BoosterType _) => _boosters++;
        private void OnDied() { if (_open) Write("lose", 0); }
        private void OnWon(bool won) { if (won && _open) Write("win", GameServices.State != null ? GameServices.State.LastStars : 0); }

        private void Write(string result, int stars)
        {
            _open = false;
            float coverage = painter != null ? painter.Coverage : 0f;
            var sb = new StringBuilder();
            sb.Append(System.DateTime.Now.ToString("s")).Append(',').Append(_campaign).Append(',').Append(_level).Append(',')
              .Append(_enemies).Append(',').Append(_sprinters).Append(',').Append(_tanks).Append(',').Append(_eaters).Append(',').Append(_dashers).Append(',')
              .Append(result).Append(',').Append((Time.time - _start).ToString("F1", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
              .Append(Mathf.RoundToInt(coverage * 100f)).Append(',').Append(_hits).Append(',').Append(_lost).Append(',').Append(_boosters).Append(',')
              .Append(stars).Append(',').Append(PlayerProgress.Coins).Append('\n');
            File.AppendAllText(_path, sb.ToString());
        }

        private static string LevelName(EdgeNetwork network)
        {
            Transform t = network.transform;
            while (t.parent != null && t.parent.parent != null) t = t.parent;   // the level group under the levels root
            return t.name;
        }
    }
}
#endif
