using UnityEngine;
using DrawRush.Boosters;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Player;

namespace DrawRush.Audio
{
    /// <summary>
    /// Plays the game's SFX purely by listening to events the systems already raise — no
    /// gameplay code calls into audio, so nothing else gains an audio dependency.
    /// Belongs on a shared object: the mega-scene never reloads, so this survives every
    /// level switch and keeps its subscriptions.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SfxPlayer : MonoBehaviour
    {
        [SerializeField] private SfxLibrary library;
        [Tooltip("Health SO to listen to. Falls back to nothing if unset — hit/lose stay silent.")]
        [SerializeField] private PlayerHealth playerHealth;
        [Tooltip("Game state SO. Falls back to GameServices.State, like the other listeners.")]
        [SerializeField] private GameState state;
        [Tooltip("Player boosters (pickup sound). Optional.")]
        [SerializeField] private PlayerBoosts boosts;
        [Tooltip("Player combat (shield break sound). Optional.")]
        [SerializeField] private PlayerCombat combat;
        [Tooltip("Free-draw painter (lost-paint sound). Optional.")]
        [SerializeField] private FreeDrawPainter painter;

        private AudioSource _source;
        private GameState _boundState;
        private PlayerHealth _boundHealth;
        private int _lastHealth = -1;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;   // feedback audio: always 2D, never positional
        }

        private void OnEnable()
        {
            _boundState = state != null ? state : GameServices.State;
            if (_boundState != null) _boundState.GameWonChanged += OnGameWonChanged;

            _boundHealth = playerHealth;
            if (_boundHealth != null)
            {
                _lastHealth = _boundHealth.Current;
                _boundHealth.Changed += OnHealthChanged;
                _boundHealth.Died += OnDied;
            }

            PlayerProgress.CoinsChanged += OnCoinsChanged;
            ProceduralWall.Revealed += OnWallRevealed;
            DrawRush.UI.ButtonJuice.Clicked += OnUiClick;
            DrawRush.Enemy.EnemyDeath.EnemyDied += OnEnemyDied;
            DrawRush.Enemy.EnemyFollow.DashWindUpStarted += OnDashWindUp;
            if (boosts != null) boosts.Granted += OnBooster;
            if (combat != null) combat.ShieldBroken += OnShieldBroken;
            if (painter != null) painter.PaintLost += OnPaintLost;
        }

        private void OnDisable()
        {
            if (_boundState != null) _boundState.GameWonChanged -= OnGameWonChanged;
            if (_boundHealth != null)
            {
                _boundHealth.Changed -= OnHealthChanged;
                _boundHealth.Died -= OnDied;
            }
            PlayerProgress.CoinsChanged -= OnCoinsChanged;
            ProceduralWall.Revealed -= OnWallRevealed;
            DrawRush.UI.ButtonJuice.Clicked -= OnUiClick;
            DrawRush.Enemy.EnemyDeath.EnemyDied -= OnEnemyDied;
            DrawRush.Enemy.EnemyFollow.DashWindUpStarted -= OnDashWindUp;
            if (boosts != null) boosts.Granted -= OnBooster;
            if (combat != null) combat.ShieldBroken -= OnShieldBroken;
            if (painter != null) painter.PaintLost -= OnPaintLost;
        }

        private void OnBooster(BoosterType _) { Play(library != null ? library.boosterPickup : null); Haptics.Light(); }
        private void OnShieldBroken() { Play(library != null ? library.shieldBreak : null); Haptics.Medium(); }
        private void OnPaintLost() => Play(library != null ? library.paintLost : null);

        // Several Dashers may flash together; one whine per moment is enough.
        private float _lastDash = -1f;
        private void OnDashWindUp()
        {
            if (Time.unscaledTime - _lastDash < 0.3f) return;
            _lastDash = Time.unscaledTime;
            Play(library != null ? library.dashWindUp : null);
        }

        // Enemies at the end of a level often die on the same frame; collapse the burst into one
        // pop so it reads as a single satisfying sound instead of a machine-gun of clips.
        private float _lastEnemyDie = -1f;
        private void OnEnemyDied()
        {
            if (Time.unscaledTime - _lastEnemyDie < 0.06f) return;
            _lastEnemyDie = Time.unscaledTime;
            Play(library != null ? library.enemyDie : null);
        }

        private void OnGameWonChanged(bool won) { if (won) Play(library != null ? library.win : null); }
        private void OnDied() => Play(library != null ? library.lose : null);
        private void OnCoinsChanged(int total) => Play(library != null ? library.coin : null);
        private void OnWallRevealed(DrawRush.Drawing.ProceduralWall wall) => Play(library != null ? library.wallRise : null);
        private void OnUiClick() => Play(library != null ? library.uiClick : null);

        // Changed also fires on heals and on the reset every level activation performs, so only
        // an actual drop counts as a hit.
        private void OnHealthChanged(int value)
        {
            if (_lastHealth >= 0 && value < _lastHealth) { Play(library != null ? library.hit : null); Haptics.Medium(); }
            _lastHealth = value;
        }

        private void Play(SfxLibrary.Cue cue)
        {
            if (cue == null || cue.clip == null || _source == null) return;
            _source.PlayOneShot(cue.clip, cue.volume);
        }
    }
}
