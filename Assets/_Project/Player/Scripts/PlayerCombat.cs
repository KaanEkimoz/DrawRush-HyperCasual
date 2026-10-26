using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using DrawRush.Core;

namespace DrawRush.Player
{
    /// <summary>
    /// Wires <see cref="PlayerHealth"/> (ScriptableObject) to the HUD label and to
    /// self-destruction. Health state lives on the asset, not in the MonoBehaviour,
    /// so other systems (enemies, UI, save/load) can read it without referencing
    /// this component.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [Tooltip("Animator that plays the hit-reaction clip. Auto-resolved from children if null.")]
        [SerializeField] private Animator playerAnim;
        [Tooltip("Damage flash + squash juice. Auto-resolved from this GameObject if null.")]
        [SerializeField] private PlayerHitFeedback hitFeedback;
        [Tooltip("Invulnerability window after taking damage, in seconds. Further hits are " +
                 "ignored until it elapses.")]
        [SerializeField] private float invulnerabilityDuration = 3f;
        [Tooltip("Invulnerable seconds after a shield absorbs a hit.")]
        [SerializeField] private float shieldGrace = 1f;

        private float _invulnerableUntil;

        /// <summary>Raised after a hit actually lands (not during invulnerability).</summary>
        public event Action Damaged;

        /// <summary>Raised when a shield charge absorbs a hit instead.</summary>
        public event Action ShieldBroken;

        /// <summary>Hits that will be absorbed without damage (Shield booster).</summary>
        public int ShieldCharges { get; set; }

        /// <summary>True while the post-hit invulnerability window is active.</summary>
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        [FormerlySerializedAs("playerHp")]
        [SerializeField] private TextMeshProUGUI healthLabel;

        private void Awake()
        {
            if (playerAnim == null) playerAnim = GetComponentInChildren<Animator>();
            if (hitFeedback == null) hitFeedback = GetComponent<PlayerHitFeedback>();
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.Changed += OnHealthChanged;
            OnHealthChanged(health.Current);
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.Changed -= OnHealthChanged;
        }

        /// <summary>Applies positive damage to the bound PlayerHealth asset, plays the
        /// hit-reaction animation/juice, and opens an invulnerability window. Hits during
        /// that window are ignored.</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || IsInvulnerable) return;

            if (ShieldCharges > 0)
            {
                ShieldCharges--;
                _invulnerableUntil = Time.time + shieldGrace;
                ShieldBroken?.Invoke();
                return;
            }

            if (health != null) health.TakeDamage(amount);
            if (playerAnim != null) playerAnim.SetTrigger(AnimatorIds.Hit);
            if (hitFeedback != null) hitFeedback.Play();
            _invulnerableUntil = Time.time + invulnerabilityDuration;
            Damaged?.Invoke();
        }

        /// <summary>Fresh-level reset: drop the post-hit invulnerability window and any running
        /// hit juice/knockback. The window is measured in scaled time, and the level-start countdown
        /// freezes time — so a hit taken just before dying/advancing used to carry its full 3 s of
        /// blinking, untouchable player into the next level.</summary>
        public void ResetForLevel()
        {
            _invulnerableUntil = 0f;
            ShieldCharges = 0;
            if (hitFeedback != null) hitFeedback.Stop();
            GetComponent<PlayerKnockback>()?.Stop();
        }

        /// <summary>Applies positive healing to the bound PlayerHealth asset.</summary>
        public void Heal(int amount)
        {
            if (health != null) health.Heal(amount);
        }

        private void OnHealthChanged(int value)
        {
            if (healthLabel != null) healthLabel.text = value.ToString();
        }
    }
}
