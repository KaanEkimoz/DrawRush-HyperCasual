using TMPro;
using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Freezes the game and shows a 3-2-1 countdown before play begins. LevelManager calls
    /// <see cref="Begin"/> whenever a level activates (start / next / restart). Runs on
    /// unscaled time so it ticks while Time.timeScale is 0, then restores timeScale to 1.
    /// A run token cancels a stale countdown if the level is re-activated mid-count.
    /// </summary>
    public sealed class LevelStartCountdown : MonoBehaviour
    {
        [SerializeField] private TMP_Text countdownText;
        [Tooltip("Number the countdown starts from (3 → 2 → 1 → go).")]
        [SerializeField] private int startFrom = 3;
        [Tooltip("Seconds each number is shown.")]
        [SerializeField] private float stepSeconds = 1f;

        private int _runToken;

        /// <summary>True from <see cref="Begin"/> until play starts (pre-level offers show then).</summary>
        public bool IsRunning { get; private set; }

        /// <summary>Freeze, count down, then unfreeze. Safe to call repeatedly; a new call
        /// supersedes any in-flight countdown.</summary>
        public void Begin()
        {
            _runToken++;
            IsRunning = true;
            MenuPause.SetGameTimeScale(0f);
            if (countdownText != null) countdownText.gameObject.SetActive(true);
            _ = RunAsync(_runToken);
        }

        private async Awaitable RunAsync(int token)
        {
            try
            {
                for (int n = startFrom; n >= 1; n--)
                {
                    if (countdownText != null) countdownText.text = n.ToString();
                    float t = 0f;
                    while (t < stepSeconds)
                    {
                        await Awaitable.NextFrameAsync(destroyCancellationToken);
                        if (token != _runToken) return;     // superseded by a newer Begin()
                        if (!MenuPause.IsOpen) t += Time.unscaledDeltaTime;   // a menu holds the count
                    }
                }
            }
            catch (System.OperationCanceledException) { return; }

            if (token != _runToken) return;                 // a newer countdown owns the state
            if (countdownText != null) countdownText.gameObject.SetActive(false);
            IsRunning = false;
            MenuPause.SetGameTimeScale(1f);
        }
    }
}
