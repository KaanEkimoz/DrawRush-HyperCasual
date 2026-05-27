using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Builds the set of unique paintable edges for the active level from the anchors'
    /// neighbor graph, and reports when every edge has been fully painted.
    ///
    /// Rebuilt on every enable so re-activating a level group in the mega-scene (where there
    /// is no scene reload) starts from a fresh, all-unpainted edge set scoped to the
    /// currently-active anchors. <see cref="UnityEngine.Object.FindObjectsByType{T}(FindObjectsSortMode)"/>
    /// excludes inactive objects, so only the active level's anchors contribute edges.
    /// </summary>
    public sealed class EdgeNetwork : MonoBehaviour
    {
        /// <summary>Raised once when the last incomplete edge becomes complete.</summary>
        public event Action AllCompleted;

        [Header("Fill visual")]
        [Tooltip("Shared material for the painted-span LineRenderers. Falls back to a " +
                 "Sprites/Default material when empty.")]
        [SerializeField] private Material fillMaterial;

        private readonly List<DrawEdge> _edges = new();
        private readonly List<GameObject> _views = new();
        private int _remaining;

        /// <summary>The edges built for the active level (read-only).</summary>
        public IReadOnlyList<DrawEdge> Edges => _edges;

        /// <summary>True once every edge is painted. False when there are no edges yet.</summary>
        public bool IsComplete => _edges.Count > 0 && _remaining == 0;

        private void OnEnable() => Rebuild();

        private void OnDisable() => UnsubscribeAll();

        /// <summary>
        /// Finds the edge connecting two anchors regardless of order. The paint controller
        /// uses this to resolve the edge the player slides along from a given corner.
        /// </summary>
        public bool TryGetEdge(DrawPart a, DrawPart b, out DrawEdge edge)
        {
            for (int i = 0; i < _edges.Count; i++)
            {
                if (_edges[i].Contains(a) && _edges[i].Contains(b))
                {
                    edge = _edges[i];
                    return true;
                }
            }
            edge = null;
            return false;
        }

        private void Rebuild()
        {
            UnsubscribeAll();
            DestroyViews();
            _edges.Clear();
            _remaining = 0;

            var parts = UnityEngine.Object.FindObjectsByType<DrawPart>(FindObjectsSortMode.None);
            if (parts.Length < 2) return;

            var indexOf = new Dictionary<DrawPart, int>(parts.Length);
            for (int i = 0; i < parts.Length; i++) indexOf[parts[i]] = i;

            var adjacency = new int[parts.Length][];
            for (int i = 0; i < parts.Length; i++)
            {
                IReadOnlyList<DrawPart> neighbors = parts[i].Neighbors;
                if (neighbors == null)
                {
                    adjacency[i] = Array.Empty<int>();
                    continue;
                }

                var row = new List<int>(neighbors.Count);
                for (int n = 0; n < neighbors.Count; n++)
                {
                    if (neighbors[n] != null && indexOf.TryGetValue(neighbors[n], out int j))
                        row.Add(j);
                }
                adjacency[i] = row.ToArray();
            }

            (int A, int B)[] pairs = DrawPartNeighborGraph.BuildUndirectedPairs(adjacency);
            for (int i = 0; i < pairs.Length; i++)
            {
                var edge = new DrawEdge(parts[pairs[i].A], parts[pairs[i].B]);
                edge.Completed += OnEdgeCompleted;
                _edges.Add(edge);
                CreateView(edge, i);
            }
            _remaining = _edges.Count;
        }

        private void CreateView(DrawEdge edge, int index)
        {
            var viewGo = new GameObject($"EdgeView_{index}");
            viewGo.transform.SetParent(transform, false);
            var view = viewGo.AddComponent<DrawEdgeView>();
            view.Bind(edge, fillMaterial);
            _views.Add(viewGo);
        }

        private void DestroyViews()
        {
            for (int i = 0; i < _views.Count; i++)
            {
                if (_views[i] != null) Destroy(_views[i]);
            }
            _views.Clear();
        }

        private void OnEdgeCompleted(DrawEdge edge)
        {
            if (_remaining > 0) _remaining--;
            if (_remaining == 0) AllCompleted?.Invoke();
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _edges.Count; i++) _edges[i].Completed -= OnEdgeCompleted;
        }
    }
}
