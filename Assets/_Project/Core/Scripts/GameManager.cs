using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Studios208.DrawRush.Enemy;
using Random = UnityEngine.Random;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Scene-level UI + level-flow controller. Listens to GameState.GameWonChanged
    /// instead of polling a static flag every Update.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [Header("UI Panel Elements"), Space]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private GameObject losePanel;
        [SerializeField] private GameObject gameUI;

        [Header("Refs"), Space]
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private GameObject winParticles;
        [SerializeField] private GameState state;

        private static readonly List<int> RandomLevelList = new() { 2, 3, 4 };
        private int _level = 1;
        private bool _lossShown;

        private void Awake()
        {
            Time.timeScale = 0f;
        }

        private void OnEnable()
        {
            if (state != null) state.GameWonChanged += OnGameWonChanged;
        }

        private void OnDisable()
        {
            if (state != null) state.GameWonChanged -= OnGameWonChanged;
        }

        private void Start()
        {
            if (gameUI != null) gameUI.SetActive(true);
            if (winPanel != null) winPanel.SetActive(false);
            if (losePanel != null) losePanel.SetActive(false);

            _level = PlayerPrefs.GetInt("Level", 1);
            if (levelText != null) levelText.text = $"Level {_level}";
        }

        private void Update()
        {
            if (_lossShown) return;
            if (GameServices.Player == null)
            {
                _lossShown = true;
                if (losePanel != null) losePanel.SetActive(true);
                Time.timeScale = 0f;
            }
        }

        private void OnGameWonChanged(bool won)
        {
            if (!won) return;

            foreach (var enemy in Object.FindObjectsByType<EnemyCombat>(FindObjectsSortMode.None))
            {
                if (enemy.EnemyAnim != null) enemy.EnemyAnim.SetTrigger("t_die");
            }

            foreach (var trail in Object.FindObjectsByType<TrailRenderer>(FindObjectsSortMode.None))
            {
                Destroy(trail);
            }

            foreach (var line in Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
            {
                Destroy(line);
            }

            if (winParticles != null) winParticles.SetActive(true);
            Invoke(nameof(ShowWinPanel), 3.0f);
        }

        private void ShowWinPanel()
        {
            if (winPanel != null) winPanel.SetActive(true);
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
