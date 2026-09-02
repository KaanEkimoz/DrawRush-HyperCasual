using UnityEngine;
using UnityEngine.EventSystems;

namespace DrawRush.UI
{
    /// <summary>
    /// Makes a UI button feel pressed: it dips down under the finger and pops back with a little
    /// overshoot on release, and fires <see cref="Clicked"/> so the audio layer can play a click
    /// (SfxPlayer listens, keeping this class free of any audio dependency — the same event-driven
    /// pattern the rest of the game's sound uses).
    ///
    /// Drop it on any Button (or any tappable graphic). Uses unscaled time so it still animates
    /// while the game is paused behind a panel or the shop.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class ButtonJuice : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Tooltip("Scale while held down.")]
        [SerializeField] private float pressedScale = 0.9f;
        [Tooltip("Overshoot scale at the peak of the release pop.")]
        [SerializeField] private float popScale = 1.08f;
        [Tooltip("How fast the scale chases its target.")]
        [SerializeField] private float speed = 14f;

        /// <summary>Raised when any juiced button is clicked — the cue for a UI click sound.</summary>
        public static event System.Action Clicked;

        private Vector3 _baseScale;
        private float _target = 1f;   // multiplier on the base scale
        private float _current = 1f;
        private bool _popping;

        private void Awake() => _baseScale = transform.localScale;
        private void OnEnable() { _current = _target = 1f; ApplyScale(); }

        public void OnPointerDown(PointerEventData _) { _target = pressedScale; _popping = false; }

        public void OnPointerUp(PointerEventData _) { _target = popScale; _popping = true; }

        public void OnPointerClick(PointerEventData _) => Clicked?.Invoke();

        private void Update()
        {
            if (Mathf.Abs(_current - _target) < 0.001f)
            {
                // Reached the overshoot peak → settle back to rest.
                if (_popping) { _popping = false; _target = 1f; }
                else return;
            }
            _current = Mathf.MoveTowards(_current, _target, speed * Time.unscaledDeltaTime * Mathf.Max(0.2f, Mathf.Abs(_current - _target) + 0.15f));
            ApplyScale();
        }

        private void ApplyScale() => transform.localScale = _baseScale * _current;
    }
}
