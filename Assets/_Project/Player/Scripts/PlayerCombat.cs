using TMPro;
using UnityEngine;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Hooks PlayerHealth (ScriptableObject) up to UI and self-destruction.
    /// Health value lives on the asset, not in the MonoBehaviour, so other systems
    /// (enemies, UI, save/load) can read it without referencing this component.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerHealth health;
        [SerializeField] private TextMeshProUGUI healthLabel;

        private void OnEnable()
        {
            if (health == null) return;
            health.Changed += OnHealthChanged;
            health.Died += OnDied;
            OnHealthChanged(health.Current);
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.Changed -= OnHealthChanged;
            health.Died -= OnDied;
        }

        public void TakeDamage(int delta)
        {
            if (health != null) health.Apply(delta);
        }

        private void OnHealthChanged(int value)
        {
            if (healthLabel != null) healthLabel.text = value.ToString();
        }

        private void OnDied()
        {
            Destroy(gameObject);
        }
    }
}
