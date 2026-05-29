using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Player
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

        private float _invulnerableUntil;

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

            if (health != null) health.TakeDamage(amount);
            if (playerAnim != null) playerAnim.SetTrigger(AnimatorIds.Hit);
            if (hitFeedback != null) hitFeedback.Play();
            _invulnerableUntil = Time.time + invulnerabilityDuration;
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
