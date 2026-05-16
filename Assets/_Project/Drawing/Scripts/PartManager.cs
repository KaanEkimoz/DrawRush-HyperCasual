using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Watches a group of DrawPart children and reveals a wall when all of them
    /// fire <see cref="DrawPart.Completed"/>. Event-driven: no Update() polling
    /// and no double-counting bugs from the previous foreach implementation.
    /// </summary>
    public sealed class PartManager : MonoBehaviour
    {
        [SerializeField] private GameObject wall;

        private readonly HashSet<DrawPart> _completed = new();
        private DrawPart[] _parts;

        private void Awake()
        {
            _parts = GetComponentsInChildren<DrawPart>(includeInactive: true);
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
            if (_completed.Count >= _parts.Length)
            {
                if (wall != null) wall.SetActive(true);
            }
        }
    }
}
