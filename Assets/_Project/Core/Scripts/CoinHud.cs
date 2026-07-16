using TMPro;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Shows the player's coin total (<see cref="PlayerProgress.Coins"/>) on a HUD label and
    /// updates it live whenever coins are earned.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class CoinHud : MonoBehaviour
    {
        private TextMeshProUGUI _label;

        private void Awake() => _label = GetComponent<TextMeshProUGUI>();

        private void OnEnable()
        {
            Refresh(PlayerProgress.Coins);
            PlayerProgress.CoinsChanged += Refresh;
        }

        private void OnDisable() => PlayerProgress.CoinsChanged -= Refresh;

        private void Refresh(int total)
        {
            if (_label != null) _label.text = total.ToString();
        }
    }
}
