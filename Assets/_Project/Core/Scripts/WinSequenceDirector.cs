using UnityEngine;
using UnityEngine.Serialization;

namespace DrawRush.Core
{
    /// <summary>
    /// Orchestrates the visual + UI side-effects of a game-won event:
    ///   1. Activate win particles
    ///   2. Wait <see cref="GameConfig.gameWonDelay"/> seconds (Awaitable)
    ///   3. Tell <see cref="HudPanels"/> to show the win panel
    ///
    /// Subscribes to <see cref="GameState.GameWonChanged"/> on enable. No polling.
    /// </summary>
    public sealed class WinSequenceDirector : MonoBehaviour
    {
        [SerializeField] private GameState state;
        [SerializeField] private HudPanels hudPanels;

        [FormerlySerializedAs("_particles")]
        [SerializeField] private GameObject winParticles;

        private GameState _bound;
        // Bumped on every win/reset so an in-flight delay can tell it has been superseded —
        // same guard LevelStartCountdown uses. Without it the win panel from a finished level
        // pops up in the middle of the NEXT one.
        private int _runToken;

        /// <summary>Late-binds inspector fields. Non-null arguments overwrite the
        /// existing serialized refs; nulls are ignored.</summary>
        public void Bind(GameState state = null, HudPanels hudPanels = null, GameObject winParticles = null)
        {
            if (state != null) this.state = state;
            if (hudPanels != null) this.hudPanels = hudPanels;
            if (winParticles != null) this.winParticles = winParticles;
        }

        private void OnEnable()
        {
            _bound = state != null ? state : GameServices.State;
            if (_bound != null) _bound.GameWonChanged += OnGameWonChanged;
        }

        private void OnDisable()
        {
            if (_bound != null) _bound.GameWonChanged -= OnGameWonChanged;
        }

        private async void OnGameWonChanged(bool won)
        {
            // Every transition invalidates any delay still counting down from a previous win.
            int token = ++_runToken;

            // Restart fires won=false on a state that was just true — turn the celebratory
            // particles back off so the next level doesn't start with them already on.
            if (!won)
            {
                if (winParticles != null) winParticles.SetActive(false);
                return;
            }
            if (winParticles != null)
            {
                winParticles.SetActive(true);
                // PlayOnAwake only fires the first time the object activates; replay
                // explicitly so confetti bursts on every win (restart, next level, …).
                var systems = winParticles.GetComponentsInChildren<ParticleSystem>(true);
                for (int i = 0; i < systems.Length; i++) { systems[i].Clear(true); systems[i].Play(true); }
            }

            float delay = GameServices.Config != null ? GameServices.Config.gameWonDelay : 3.0f;
            try
            {
                await Awaitable.WaitForSecondsAsync(delay, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            // A newer win/reset happened while we waited (player hit Restart/Next, or the level
            // switched) — that run owns the UI now, so don't slam a stale panel over it.
            if (token != _runToken) return;
            if (hudPanels != null) hudPanels.ShowWinPanel();
        }
    }
}
