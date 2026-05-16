using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Reveals a wall when every child DrawPart completes. Delegates the bookkeeping
    /// to <see cref="DrawPartCompletionWatcher"/> — 90% of the legacy logic now lives
    /// in the shared helper.
    /// </summary>
    public sealed class PartManager : MonoBehaviour
    {
        [SerializeField] private GameObject wall;

        private DrawPartCompletionWatcher _watcher;

        private void Awake()
        {
            var components = GetComponentsInChildren<DrawPart>(includeInactive: true);
            var parts = new IDrawPart[components.Length];
            for (int i = 0; i < components.Length; i++) parts[i] = components[i];
            _watcher = new DrawPartCompletionWatcher(parts);
            _watcher.AllCompleted += OnAllCompleted;
        }

        private void OnEnable() => _watcher?.Enable();
        private void OnDisable() => _watcher?.Disable();

        private void OnAllCompleted()
        {
            if (wall != null) wall.SetActive(true);
        }
    }
}
