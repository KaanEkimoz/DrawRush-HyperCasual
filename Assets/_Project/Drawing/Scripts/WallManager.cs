using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Flips <see cref="GameState.IsGameWon"/> when every scene-wide DrawPart is
    /// complete. Delegates the bookkeeping to <see cref="DrawPartCompletionWatcher"/>.
    /// Renaming to WinCondition is queued for a separate commit — keeps scene refs
    /// stable.
    /// </summary>
    public sealed class WallManager : MonoBehaviour
    {
        private DrawPartCompletionWatcher _watcher;

        private void Awake()
        {
            var components = Object.FindObjectsByType<DrawPart>(FindObjectsSortMode.None);
            var parts = new IDrawPart[components.Length];
            for (int i = 0; i < components.Length; i++) parts[i] = components[i];
            _watcher = new DrawPartCompletionWatcher(parts);
            _watcher.AllCompleted += OnAllCompleted;
        }

        private void OnEnable() => _watcher?.Enable();
        private void OnDisable() => _watcher?.Disable();

        private void OnAllCompleted()
        {
            if (GameServices.State != null)
            {
                GameServices.State.IsGameWon = true;
            }
        }
    }
}
