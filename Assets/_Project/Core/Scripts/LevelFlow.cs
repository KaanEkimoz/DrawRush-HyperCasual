using UnityEngine;
using Random = UnityEngine.Random;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Owns level navigation + level-index persistence. In the mega-scene
    /// architecture there is no per-level scene reload: navigation delegates to
    /// <see cref="LevelManager"/>, which enables one level group at a time and
    /// resets per-level state. PlayerPrefs still persists the player's progress.
    /// </summary>
    public sealed class LevelFlow : MonoBehaviour
    {
        [Tooltip("PlayerPrefs key used to persist the current level number.")]
        [SerializeField] private string playerPrefsKey = "Level";

        [Tooltip("Switcher that enables level groups in the mega-scene.")]
        [SerializeField] private LevelManager levelManager;

        public int CurrentLevel => PlayerPrefs.GetInt(playerPrefsKey, 1);

        public void StartTheGame()
        {
            Time.timeScale = 1.0f;
        }

        public void RestartLevel()
        {
            if (levelManager == null) return;
            levelManager.ActivateLevel(levelManager.CurrentIndex);
        }

        public void NextLevel()
        {
            if (levelManager == null) return;

            int next = levelManager.CurrentIndex + 1;
            if (next >= levelManager.LevelCount)
            {
                // Past the final level — fall back to the random cycler so the player
                // never dead-ends after finishing the campaign.
                LoadRandomLevel();
                return;
            }
            PlayerPrefs.SetInt(playerPrefsKey, CurrentLevel + 1);
            levelManager.ActivateLevel(next);
        }

        public void LoadRandomLevel()
        {
            if (levelManager == null || levelManager.LevelCount == 0) return;

            PlayerPrefs.SetInt(playerPrefsKey, CurrentLevel + 1);

            int current = levelManager.CurrentIndex;
            int pick = Random.Range(0, levelManager.LevelCount);
            while (pick == current && levelManager.LevelCount > 1)
            {
                pick = Random.Range(0, levelManager.LevelCount);
            }
            levelManager.ActivateLevel(pick);
        }
    }
}
