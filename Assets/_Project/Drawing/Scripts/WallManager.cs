using System.Collections.Generic;
using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Watches every DrawPart in the scene and flips GameState.IsGameWon when all of
    /// them complete. Event-driven and decoupled from any specific PartManager — works
    /// when parts are spread across multiple manager groups.
    /// </summary>
    public sealed class WallManager : MonoBehaviour
    {
        private readonly HashSet<DrawPart> _completed = new();
        private DrawPart[] _parts;

        private void Awake()
        {
            _parts = Object.FindObjectsByType<DrawPart>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].Completed += OnPartCompleted;
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < _parts.Length; i++)
            {
                _parts[i].Completed -= OnPartCompleted;
            }
        }

        private void OnPartCompleted(DrawPart part)
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
