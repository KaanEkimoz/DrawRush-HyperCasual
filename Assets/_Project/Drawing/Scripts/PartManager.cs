using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Reveals a wall when all child DrawParts complete. Subscribes to
    /// <see cref="IDrawPart.Completed"/> instead of polling.
    /// </summary>
    public sealed class PartManager : MonoBehaviour
    {
        [SerializeField] private GameObject wall;

        private readonly HashSet<IDrawPart> _completed = new();
        private IDrawPart[] _parts;

        private void Awake()
        {
            var components = GetComponentsInChildren<DrawPart>(includeInactive: true);
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
            if (_completed.Count >= _parts.Length && wall != null)
            {
                wall.SetActive(true);
            }
        }
    }
}
