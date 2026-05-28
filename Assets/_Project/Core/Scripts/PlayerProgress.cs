using UnityEngine;

namespace Studios208.DrawRush.Core
{
    /// <summary>
    /// Player-local persistent progress, backed by <see cref="PlayerPrefs"/> so it survives
    /// app restarts and reinstalls-aside is per-device. Currently records whether the tutorial
    /// has been completed, so it is shown once to a new player and skipped from then on.
    /// </summary>
    public static class PlayerProgress
    {
        private const string TutorialCompletedKey = "TutorialCompleted";

        /// <summary>True once the player has finished the tutorial level.</summary>
        public static bool TutorialCompleted
        {
            get => PlayerPrefs.GetInt(TutorialCompletedKey, 0) == 1;
            set
            {
                PlayerPrefs.SetInt(TutorialCompletedKey, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }
}
