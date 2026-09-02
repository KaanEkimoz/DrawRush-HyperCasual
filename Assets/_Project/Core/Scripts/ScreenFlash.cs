using UnityEngine;
using UnityEngine.UI;
using DrawRush.Player;

namespace DrawRush.Core
{
    /// <summary>
    /// A full-screen colour flash for the two moments that most need to land: getting hit (a red
    /// pulse, so damage is felt and not just a number ticking down) and winning (a soft white pop
    /// under the confetti). Drives one canvas Image's alpha; it listens to the same health and
    /// game-state signals everything else does, so nothing calls into it.
    /// </summary>
    public sealed class ScreenFlash : MonoBehaviour
    {
        [SerializeField] private Image flash;
        [SerializeField] private Color hitColor = new Color(1f, 0.1f, 0.1f, 1f);
        [SerializeField] private Color winColor = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private float hitPeak = 0.5f;
        [SerializeField] private float winPeak = 0.6f;
        [SerializeField] private float fade = 2.5f;   // alpha units per second

        private PlayerHealth _health;
        private GameState _state;
        private int _lastHealth = -1;
        private float _alpha;
        private Color _target;

        private void OnEnable()
        {
            _health = GameServices.Health;
            if (_health != null) { _lastHealth = _health.Current; _health.Changed += OnHealthChanged; }
            _state = GameServices.State;
            if (_state != null) _state.GameWonChanged += OnGameWon;
            SetAlpha(0f);
        }

        private void OnDisable()
        {
            if (_health != null) _health.Changed -= OnHealthChanged;
            if (_state != null) _state.GameWonChanged -= OnGameWon;
        }

        private void OnHealthChanged(int value)
        {
            // Changed also fires on heals and the per-level reset; only a real drop is a hit.
            if (_lastHealth >= 0 && value < _lastHealth) Trigger(hitColor, hitPeak);
            _lastHealth = value;
        }

        private void OnGameWon(bool won) { if (won) Trigger(winColor, winPeak); }

        private void Trigger(Color color, float peak)
        {
            _target = color;
            _alpha = Mathf.Max(_alpha, peak);
            if (flash != null) { flash.color = new Color(color.r, color.g, color.b, _alpha); flash.enabled = true; }
        }

        private void Update()
        {
            if (_alpha <= 0f || flash == null) return;
            // Unscaled so a flash still fades while the countdown or shop has time paused.
            _alpha = Mathf.MoveTowards(_alpha, 0f, fade * Time.unscaledDeltaTime);
            SetAlpha(_alpha);
        }

        private void SetAlpha(float a)
        {
            if (flash == null) return;
            flash.color = new Color(_target.r, _target.g, _target.b, a);
            if (a <= 0f) flash.enabled = false;
        }
    }
}
