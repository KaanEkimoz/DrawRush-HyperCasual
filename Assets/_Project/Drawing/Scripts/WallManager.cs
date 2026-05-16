using System.Collections.Generic;
using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Scene-wide completion watcher. Flips <see cref="GameState.IsGameWon"/> when
    /// every DrawPart in the scene completes. Event-driven via <see cref="IDrawPart.Completed"/>.
    /// </summary>
    public sealed class WallManager : MonoBehaviour
    {
        private readonly HashSet<IDrawPart> _completed = new();
        private IDrawPart[] _parts;

        private void Awake()
        {
            var components = Object.FindObjectsByType<DrawPart>(FindObjectsSortMode.None);
            _parts = new IDrawPart[components.Length];
            for (int i = 0; i < components.Length; i++) _parts[i] = components[i];
        }

        private void OnEnable()
        {
            for (int i = 0; i < _parts.Length; i++) _parts[i].Completed += OnPartCompleted;
        }

        private void OnDisable()
        {
            for (int i = 0; i < _parts.Length; i++) _parts[i].Completed -= OnPartCompleted;
        }

        private void OnPartCompleted(IDrawPart part)
        {
            _completed.Add(part);
            if (_completed.Count < _parts.Length) return;
            if (GameServices.State != null)
            {
                GameServices.State.IsGameWon = true;
            }
        }
    }
}
