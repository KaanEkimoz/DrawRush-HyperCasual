using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Studios208.DrawRush.Player;
using Random = UnityEngine.Random;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Scene-level UI panels + level flow.
    ///
    /// No longer reaches into Enemy / Drawing layers — it just listens to
    /// <see cref="GameState.GameWonChanged"/> and <see cref="PlayerHealth.Died"/>
    /// and toggles its own panels. Trail / line / enemy-anim cleanup is now owned
    /// by those features themselves.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [Header("UI Panel Elements"), Space]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject gameUI;

        [Header("Refs"), Space]
        [FormerlySerializedAs("_levelText")]
        [SerializeField] private TextMeshProUGUI levelText;
        [FormerlySerializedAs("_particles")]
        [SerializeField] private GameObject winParticles;
        [SerializeField] private GameState state;
        [SerializeField] private PlayerHealth playerHealth;

        private static readonly List<int> RandomLevelList = new() { 2, 3, 4 };
        private int _level = 1;

        private void Awake()
        {
            Time.timeScale = 0f;
        }

        private void OnEnable()
        {
            if (state != null) state.GameWonChanged += OnGameWonChanged;
            if (playerHealth != null) playerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            if (state != null) state.GameWonChanged -= OnGameWonChanged;
            if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
        }

        private void Start()
        {
            if (gameUI != null) gameUI.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            _level = PlayerPrefs.GetInt("Level", 1);
            if (levelText != null) levelText.text = $"Level {_level}";
        }

        private async void OnGameWonChanged(bool won)
        {
            if (!won) return;

            if (winParticles != null) winParticles.SetActive(true);

            float delay = GameServices.Config != null ? GameServices.Config.gameWonDelay : 3.0f;
            try
            {
                await Awaitable.WaitForSecondsAsync(delay, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            if (winPanel != null) winPanel.SetActive(true);
        }

        private void OnPlayerDied()
        {
            if (losePanel != null) losePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void StartTheGame()
        {
            Time.timeScale = 1.0f;
        }

        public void ResetTheGame()
        {
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);
        }

        #region SceneManagement

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void NextLevel()
        {
            PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void LoadRandomLevel()
        {
            PlayerPrefs.SetInt("Level", PlayerPrefs.GetInt("Level") + 1);

            if (RandomLevelList.Count == 0)
            {
                RandomLevelList.Add(2);
                RandomLevelList.Add(3);
                RandomLevelList.Add(4);
            }

            int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            int pickIndex = Random.Range(0, RandomLevelList.Count);
            while (RandomLevelList[pickIndex] == currentBuildIndex && RandomLevelList.Count > 1)
            {
                pickIndex = Random.Range(0, RandomLevelList.Count);
            }

            int chosenScene = RandomLevelList[pickIndex];
            RandomLevelList.RemoveAt(pickIndex);
            SceneManager.LoadScene(chosenScene);
        }

        #endregion
    }
}
