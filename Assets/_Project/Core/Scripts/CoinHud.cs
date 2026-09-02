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
            if (_label != null) _label.text = Abbreviate(total);
        }

        /// <summary>Idle-game number formatting: 1,234 -> "1.2K", 1,200,000 -> "1.2M". Keeps the
        /// coin counter short and readable instead of sprawling to eight digits.</summary>
        public static string Abbreviate(int value)
        {
            if (value < 1000) return value.ToString();
            if (value < 1_000_000) return Trim(value / 1000f) + "K";
            return Trim(value / 1_000_000f) + "M";
        }

        // One decimal, but drop a trailing ".0" so it reads "5K" not "5.0K".
        private static string Trim(float v)
        {
            string s = v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
            return s.EndsWith(".0") ? s.Substring(0, s.Length - 2) : s;
        }
    }
}
