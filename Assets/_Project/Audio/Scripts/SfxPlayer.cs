using UnityEngine;
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
            if (_lastHealth >= 0 && value < _lastHealth) Play(library != null ? library.hit : null);
            _lastHealth = value;
        }

        private void Play(SfxLibrary.Cue cue)
        {
            if (cue == null || cue.clip == null || _source == null) return;
            _source.PlayOneShot(cue.clip, cue.volume);
        }
    }
}
