using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// HUD panel visibility + level label. Pure presentation — no game-flow logic.
    /// Extracted from the legacy god-class GameManager. Supports late binding via
    /// <see cref="Bind"/> so a GameManager facade can forward its legacy fields
    /// without requiring scene-level re-wiring.
    /// </summary>
    public sealed class HudPanels : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject gameUI;

        [Header("Labels")]
        [FormerlySerializedAs("_levelText")]
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Format")]
        [SerializeField] private string levelLabelFormat = "Level {0}";

        private void Start()
        {
            if (gameUI != null) gameUI.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
        }

        /// <summary>Late-binds inspector fields. Non-null arguments overwrite the
        /// existing serialized refs; nulls are ignored.</summary>
        public void Bind(GameObject winPanel = null, GameObject losePanel = null,
                         GameObject gameUI = null, TextMeshProUGUI levelText = null)
        {
            if (winPanel != null) this.winPanel = winPanel;
            if (losePanel != null) this.losePanel = losePanel;
            if (gameUI != null) this.gameUI = gameUI;
            if (levelText != null) this.levelText = levelText;
        }

        public void SetLevelLabel(int level)
        {
            if (levelText != null) levelText.text = string.Format(levelLabelFormat, level);
        }

        public void ShowWinPanel()
        {
            if (winPanel != null) winPanel.SetActive(true);
        }

        public void ShowLosePanel()
        {
            if (losePanel != null) losePanel.SetActive(true);
        }

        public void HideAllPanels()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
        }
    }
}
