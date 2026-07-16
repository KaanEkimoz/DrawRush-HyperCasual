using TMPro;
using UnityEngine;
using DrawRush.Player;

namespace DrawRush.Core
{
    /// <summary>
    /// Binds a HUD label to the player's remaining health (<see cref="PlayerHealth.Current"/>)
    /// and updates it live on every change. Mirrors <see cref="CoinHud"/>, but health is a
    /// ScriptableObject instance (not a static), so the reference is serialized and wired to
    /// PlayerHealth.asset in the scene.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class HealthHud : MonoBehaviour
    {
        [Tooltip("The shared PlayerHealth SO whose Current value this label mirrors.")]
        [SerializeField] private PlayerHealth health;

        private TextMeshProUGUI _label;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        private void OnEnable()
        {
            if (health == null) return;
            // Current is 0 until ResetToStarting() runs on the first level activation — show the
            // starting value meanwhile so the HUD never flashes "0" at boot.
            Refresh(health.Current > 0 ? health.Current : health.StartingValue);
            health.Changed += Refresh;
        }

        private void OnDisable()
        {
            if (health != null) health.Changed -= Refresh;
        }

        private void Refresh(int value)
        {
            if (_label != null) _label.text = value.ToString();
        }
    }
}
