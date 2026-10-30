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

        [Tooltip("Seconds the drawing sound takes to fade in and out.")]
        [SerializeField] private float drawFade = 0.08f;
        [Tooltip("At most one enemy noise this often (seconds): a crowd must not turn into a racket.")]
        [SerializeField] private float voiceGap = 0.4f;

        private AudioSource _source, _drawSource;
        private float _lastVoice = -1f;
        private GameState _boundState;
        private PlayerHealth _boundHealth;
        private int _lastHealth = -1;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;   // feedback audio: always 2D, never positional
            _drawSource = gameObject.AddComponent<AudioSource>();
            _drawSource.playOnAwake = false;
            _drawSource.loop = true;
            _drawSource.spatialBlend = 0f;
            _drawSource.volume = 0f;
            _drawSource.outputAudioMixerGroup = _source.outputAudioMixerGroup;
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
            DrawRush.Drawing.DrawEdgeAuthor.EdgeFinished += OnEdgeFinished;
            DrawRush.UI.ButtonJuice.Clicked += OnUiClick;
            DrawRush.Enemy.EnemyDeath.EnemyDied += OnEnemyDied;
            DrawRush.Enemy.EnemyFollow.DashWindUpStarted += OnDashWindUp;
            DrawRush.Enemy.EnemyFollow.Voiced += OnEnemyVoiced;
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
            DrawRush.Drawing.DrawEdgeAuthor.EdgeFinished -= OnEdgeFinished;
            DrawRush.UI.ButtonJuice.Clicked -= OnUiClick;
            DrawRush.Enemy.EnemyDeath.EnemyDied -= OnEnemyDied;
            DrawRush.Enemy.EnemyFollow.DashWindUpStarted -= OnDashWindUp;
            DrawRush.Enemy.EnemyFollow.Voiced -= OnEnemyVoiced;
            if (boosts != null) boosts.Granted -= OnBooster;
            if (combat != null) combat.ShieldBroken -= OnShieldBroken;
            if (painter != null) painter.PaintLost -= OnPaintLost;
        }

        // The pencil scratch follows the brush: it runs only while paint is actually going down.
        private void Update()
        {
            if (library == null || library.drawLoop == null || library.drawLoop.clip == null) return;
            if (_drawSource.clip == null) { _drawSource.clip = library.drawLoop.clip; _drawSource.Play(); }
            bool on = painter != null && painter.IsPainting && Time.timeScale > 0f;
            float target = on ? library.drawLoop.volume : 0f;
            _drawSource.mute = _source.mute;
            _drawSource.volume = Mathf.MoveTowards(_drawSource.volume, target, Time.unscaledDeltaTime / Mathf.Max(0.01f, drawFade));
        }

        private void OnEnemyVoiced(DrawRush.Enemy.EnemyVariant variant)
        {
            if (library == null || library.enemyVoices == null || (int)variant >= library.enemyVoices.Length) return;
            if (Time.unscaledTime - _lastVoice < voiceGap) return;
            _lastVoice = Time.unscaledTime;
            Play(library.enemyVoices[(int)variant]);
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
        private void OnEdgeFinished(Vector3 _, Color __) => Play(library != null ? library.wallRise : null);
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
