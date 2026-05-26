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

        private void Awake()
        {
            // LevelFlow may be added at runtime by GameManager, so the Inspector
            // wiring can be absent — resolve the scene's LevelManager as a fallback.
            if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        }

        public void StartTheGame()
        {
            Time.timeScale = 1.0f;
        }

        public void RestartLevel()
        {
            if (levelManager == null) return;
            Time.timeScale = 1f;   // in-scene switch: no scene reload to un-pause for us
            levelManager.ActivateLevel(levelManager.CurrentIndex);
        }

        public void NextLevel()
        {
            if (levelManager == null) return;
            Time.timeScale = 1f;

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
            Time.timeScale = 1f;

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
