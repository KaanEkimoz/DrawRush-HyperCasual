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

        // Rebuilt on every enable so re-activating a level group in the mega-scene
        // (where there is no scene reload) starts from a fresh, all-idle watcher
        // scoped to the currently-active DrawParts. FindObjectsByType excludes
        // inactive objects by default, so only the active level's anchors count.
        private void OnEnable()
        {
            RebuildWatcher();
            _watcher.Enable();
        }

        private void OnDisable() => _watcher?.Disable();

        private void RebuildWatcher()
        {
            if (_watcher != null) _watcher.AllCompleted -= OnAllCompleted;

            var components = Object.FindObjectsByType<DrawPart>(FindObjectsSortMode.None);
            var parts = new IDrawPart[components.Length];
            for (int i = 0; i < components.Length; i++) parts[i] = components[i];
            _watcher = new DrawPartCompletionWatcher(parts);
            _watcher.AllCompleted += OnAllCompleted;
        }

        private void OnAllCompleted()
        {
            if (GameServices.State != null)
            {
                GameServices.State.IsGameWon = true;
            }
        }
    }
}
