using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Owns scene navigation + level-index persistence. Extracted from the legacy
    /// god-class GameManager. Stateless apart from the static random-level pool
    /// which is preserved for backwards-compatible level cycling.
    /// </summary>
    public sealed class LevelFlow : MonoBehaviour
    {
        [Tooltip("PlayerPrefs key used to persist the current level number.")]
        [SerializeField] private string playerPrefsKey = "Level";

        [Tooltip("Build indices used by LoadRandomLevel.")]
        [SerializeField] private int[] randomPoolBuildIndices = { 2, 3, 4 };

        private static List<int> s_randomPool;

        public int CurrentLevel => PlayerPrefs.GetInt(playerPrefsKey, 1);

        public void StartTheGame()
        {
            Time.timeScale = 1.0f;
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void NextLevel()
        {
            PlayerPrefs.SetInt(playerPrefsKey, CurrentLevel + 1);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }

        public void LoadRandomLevel()
        {
            PlayerPrefs.SetInt(playerPrefsKey, CurrentLevel + 1);
            s_randomPool ??= new List<int>(randomPoolBuildIndices);
            if (s_randomPool.Count == 0) s_randomPool.AddRange(randomPoolBuildIndices);

            int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
            int pickIndex = Random.Range(0, s_randomPool.Count);
            while (s_randomPool[pickIndex] == currentBuildIndex && s_randomPool.Count > 1)
            {
                pickIndex = Random.Range(0, s_randomPool.Count);
            }
            int chosenScene = s_randomPool[pickIndex];
            s_randomPool.RemoveAt(pickIndex);
            SceneManager.LoadScene(chosenScene);
        }
    }
}
