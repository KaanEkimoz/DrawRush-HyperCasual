using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Collects the active level's authored edges (<see cref="DrawEdgeAuthor"/> "Kenar" prefabs)
    /// and tracks completion. Each author already knows its two endpoint anchors, so no neighbor
    /// graph is computed. When an edge fills, its author reveals its wall segment; when every
    /// edge fills, <see cref="AllCompleted"/> fires (the win condition listens).
    ///
    /// Rebuilt on every enable so re-activating a level in the mega-scene starts from a fresh,
    /// unpainted edge set scoped to the active authors (FindObjectsByType excludes inactive).
    /// </summary>
    public sealed class EdgeNetwork : MonoBehaviour
    {
        /// <summary>Raised once when the last incomplete edge becomes complete.</summary>
        public event Action AllCompleted;

        [Tooltip("Fallback painted-line color when an edge's wall has no readable color.")]
        [SerializeField] private Color fallbackLineColor = new Color(0.1f, 1f, 0.8f, 1f);

        private readonly List<DrawEdge> _edges = new();
        private readonly Dictionary<DrawEdge, DrawEdgeAuthor> _authors = new();
        private int _remaining;

        /// <summary>The edges built for the active level (read-only).</summary>
        public IReadOnlyList<DrawEdge> Edges => _edges;

        /// <summary>True once every edge is painted. False when there are no edges yet.</summary>
        public bool IsComplete => _edges.Count > 0 && _remaining == 0;

        private void OnEnable() => Rebuild();

        private void OnDisable() => UnsubscribeAll();

        /// <summary>Appends every edge touching <paramref name="anchor"/> to
        /// <paramref name="results"/> (cleared first). The paint controller uses this to pick
        /// which edge to slide along from a corner.</summary>
        public void GetEdgesTouching(DrawPart anchor, List<DrawEdge> results)
        {
            results.Clear();
            for (int i = 0; i < _edges.Count; i++)
                if (_edges[i].Contains(anchor)) results.Add(_edges[i]);
        }

        private void Rebuild()
        {
            UnsubscribeAll();
            _edges.Clear();
            _authors.Clear();
            _remaining = 0;

            var authors = UnityEngine.Object.FindObjectsByType<DrawEdgeAuthor>(FindObjectsSortMode.None);
            for (int i = 0; i < authors.Length; i++)
            {
                DrawEdgeAuthor author = authors[i];
                if (!author.IsValid) continue;

                var edge = new DrawEdge(author.AnchorA, author.AnchorB);
                edge.Completed += OnEdgeCompleted;
                _edges.Add(edge);
                _authors[edge] = author;
                author.View.Bind(edge, author.WallColor(fallbackLineColor));
            }
            _remaining = _edges.Count;
        }

        private void OnEdgeCompleted(DrawEdge edge)
        {
            if (_authors.TryGetValue(edge, out DrawEdgeAuthor author)) author.Reveal();
            if (_remaining > 0) _remaining--;
            if (_remaining == 0) AllCompleted?.Invoke();
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _edges.Count; i++) _edges[i].Completed -= OnEdgeCompleted;
        }
    }
}
