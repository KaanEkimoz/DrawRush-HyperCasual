using UnityEngine;
using UnityEngine.SceneManagement;

namespace DrawRush.Utilities
{
    /// <summary>
    /// Waits a fixed delay (default 2.1s) on the splash scene then loads the next
    /// build-index scene. Uses Awaitable so it lives outside coroutine state machines
    /// and is cancelled if the GameObject is destroyed mid-wait.
    /// </summary>
    public sealed class VideoEnd : MonoBehaviour
    {
        [SerializeField] private float waitSeconds = 2.1f;

        private async void Start()
        {
            try
            {
                await Awaitable.WaitForSecondsAsync(waitSeconds, destroyCancellationToken);
            }
            catch (System.OperationCanceledException)
            {
                return;
            }
            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(next);
            }
        }
    }
}
