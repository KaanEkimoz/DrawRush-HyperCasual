using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Reveals this part's wall once every paintable edge belonging to its anchors is
    /// filled. In the edge-painting model anchors are wired into edges by the level's
    /// <see cref="EdgeNetwork"/>; this watches the edges whose both endpoints are this
    /// part's child anchors and activates the wall — whose Animator plays the reveal clip
    /// on enable — when they are all complete.
    /// </summary>
    public sealed class PartManager : MonoBehaviour
    {
        [SerializeField] private GameObject wall;

        private EdgeNetwork _network;
        private readonly HashSet<DrawPart> _myParts = new();
        private bool _revealed;

        private void OnEnable()
        {
            _revealed = false;
            _myParts.Clear();
            foreach (DrawPart p in GetComponentsInChildren<DrawPart>(includeInactive: true)) _myParts.Add(p);

            // Start hidden. The wall is revealed only when this part's edges are painted;
            // hiding here also resets a previously-revealed wall when the level restarts.
            if (wall != null) wall.SetActive(false);

            // Reveal happens via EdgeCompleted (fired after a real paint, post-rebuild) so we
            // never read a stale, pre-restart edge set.
            _network = ResolveNetwork();
            if (_network != null) _network.EdgeCompleted += OnEdgeCompleted;
        }

        private void OnDisable()
        {
            if (_network != null) _network.EdgeCompleted -= OnEdgeCompleted;
        }

        private void OnEdgeCompleted(DrawEdge edge) => TryReveal();

        private void TryReveal()
        {
            if (_revealed || _network == null) return;

            bool anyMine = false;
            IReadOnlyList<DrawEdge> edges = _network.Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                DrawEdge e = edges[i];
                if (!_myParts.Contains(e.A) || !_myParts.Contains(e.B)) continue;   // not this part's edge
                anyMine = true;
                if (!e.IsComplete) return;   // one of my edges is still unpainted
            }
            if (!anyMine) return;            // edges not built yet, or none belong to me

            _revealed = true;
            if (wall != null) wall.SetActive(true);   // Animator plays the reveal on enable
        }

        private EdgeNetwork ResolveNetwork()
        {
            Transform t = transform;
            while (t != null && !t.name.StartsWith("Level_", StringComparison.Ordinal)) t = t.parent;
            if (t != null)
            {
                EdgeNetwork scoped = t.GetComponentInChildren<EdgeNetwork>();
                if (scoped != null) return scoped;
            }
            return UnityEngine.Object.FindFirstObjectByType<EdgeNetwork>();
        }
    }
}
