using System;
using System.Collections.Generic;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Watches a set of <see cref="IDrawPart"/>s and raises <see cref="AllCompleted"/>
    /// exactly once when every part has reported Completed. Shared by PartManager
    /// (group-of-children) and WallManager (scene-wide) — extracts the duplicated
    /// HashSet + subscribe/unsubscribe boilerplate.
    /// </summary>
    public sealed class DrawPartCompletionWatcher
    {
        public event Action AllCompleted;

        private readonly IReadOnlyList<IDrawPart> _parts;
        private readonly HashSet<IDrawPart> _completed = new();
        private bool _finalized;
        private bool _enabled;

        public DrawPartCompletionWatcher(IReadOnlyList<IDrawPart> parts)
        {
            _parts = parts ?? throw new ArgumentNullException(nameof(parts));
        }

        public int Total => _parts.Count;
        public int CompletedCount => _completed.Count;
        public bool IsFinalized => _finalized;

        public void Enable()
        {
            if (_enabled) return;
            _enabled = true;
            for (int i = 0; i < _parts.Count; i++) _parts[i].Completed += OnPartCompleted;
        }

        public void Disable()
        {
            if (!_enabled) return;
            _enabled = false;
            for (int i = 0; i < _parts.Count; i++) _parts[i].Completed -= OnPartCompleted;
        }

        private void OnPartCompleted(IDrawPart part)
        {
            if (_finalized) return;
            _completed.Add(part);
            if (_completed.Count < _parts.Count) return;
            _finalized = true;
            AllCompleted?.Invoke();
        }
    }
}
