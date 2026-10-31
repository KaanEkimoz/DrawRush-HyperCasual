using System;
using DrawRush.Enemy;
using UnityEngine;

namespace DrawRush.Player
{
    /// <summary>
    /// The hero's last moment: the pencil flies up spinning, limbs flailing, drops flat on its back
    /// with crossed-out eyes and bounces once. Runs on unscaled time because the game is already
    /// frozen, and the lose panel opens partway through so the two play together.
    /// </summary>
    public sealed class PencilDeath : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private HopAnimator pencil;
        [SerializeField] private Animator limbs;
        [SerializeField] private Renderer eyes;
        [SerializeField] private Material deadEyes;
        [Tooltip("Whole animation, seconds.")]
        [SerializeField] private float duration = 1.25f;
        [Tooltip("Share of the animation spent in the air (0-1).")]
        [SerializeField, Range(0.2f, 0.9f)] private float airShare = 0.6f;
        [Tooltip("Seconds in before the lose panel opens.")]
        [SerializeField] private float panelDelay = 0.45f;
        [SerializeField] private float jumpHeight = 1.6f;
        [SerializeField] private float spinTurns = 1.5f;
        [Tooltip("How far (parent units) the lying pencil's pivot sits above its feet.")]
        [SerializeField] private float lyingHeight = 0.25f;
        [SerializeField] private float bounceHeight = 0.3f;

        private Transform _hero;
        private Material _aliveEyes;
        private Action _openPanel;
        private Vector3 _start;
        private float _time;
        private bool _playing;

        /// <summary>Starts the animation; <paramref name="openPanel"/> runs partway through.</summary>
        public void Play(Action openPanel)
        {
            _hero = pencil.transform;
            _start = _hero.localPosition;
            _openPanel = openPanel;
            _time = 0f;
            _playing = true;
            pencil.enabled = false;
            if (limbs != null)
            {
                limbs.updateMode = AnimatorUpdateMode.UnscaledTime;
                limbs.speed = 1.8f;
                limbs.SetBool(HopAnimator.MovingId, true);
            }
            _aliveEyes = eyes.sharedMaterial;
            eyes.sharedMaterial = deadEyes;
        }

        private void OnEnable()
        {
            if (health != null) health.Changed += OnHealthChanged;
        }

        private void OnDisable()
        {
            if (health != null) health.Changed -= OnHealthChanged;
        }

        // Any way back to life (retry, revive, next level) refills health.
        private void OnHealthChanged(int value)
        {
            if (value <= 0 || pencil == null || pencil.enabled) return;
            _playing = false;
            pencil.enabled = true;
            eyes.sharedMaterial = _aliveEyes;
            if (limbs != null) limbs.updateMode = AnimatorUpdateMode.Normal;
        }

        private void LateUpdate()
        {
            if (!_playing) return;
            // The last hit opened an invulnerability blink that would freeze with the game.
            if (_time == 0f && combat != null) combat.ResetForLevel();
            _time += Time.unscaledDeltaTime;
            if (_openPanel != null && _time >= panelDelay)
            {
                _openPanel();
                _openPanel = null;
            }

            float k = Mathf.Clamp01(_time / duration);
            float air = Mathf.Clamp01(k / airShare);
            float rest = Mathf.Clamp01((k - airShare) / (1f - airShare));
            float height = air < 1f
                ? Mathf.Sin(air * Mathf.PI) * jumpHeight + air * lyingHeight
                : lyingHeight + Mathf.Sin(rest * Mathf.PI) * bounceHeight * (1f - rest);
            _hero.localPosition = _start + Vector3.up * height;
            _hero.localRotation = Quaternion.Euler(-90f * Mathf.SmoothStep(0f, 1f, air), 360f * spinTurns * (1f - air) * air * 4f, 0f);

            if (limbs != null && air >= 1f) limbs.SetBool(HopAnimator.MovingId, false);
            if (k >= 1f) _playing = false;
        }
    }
}
