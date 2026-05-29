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

        [FormerlySerializedAs("playerHp")]
        [SerializeField] private TextMeshProUGUI healthLabel;

        private void Awake()
        {
            if (playerAnim == null) playerAnim = GetComponentInChildren<Animator>();
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

        /// <summary>Applies positive damage to the bound PlayerHealth asset and plays
        /// the hit-reaction animation (one-shot).</summary>
        public void TakeDamage(int amount)
        {
            if (health != null) health.TakeDamage(amount);
            if (playerAnim != null && amount > 0) playerAnim.SetTrigger(AnimatorIds.Hit);
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
