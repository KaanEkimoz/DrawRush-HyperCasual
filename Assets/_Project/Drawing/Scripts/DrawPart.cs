using System;
using System.Collections.Generic;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A polygon-corner anchor for the edge-painting puzzle. Holds its edge-neighbor set
    /// (auto-wired to the two nearest sibling anchors when left empty) and toggles an
    /// optional highlight while the player is touching it. Edges, fill state and completion
    /// live in <see cref="EdgeNetwork"/> / <see cref="DrawEdge"/> — DrawPart is just the corner.
    /// </summary>
    public sealed class DrawPart : MonoBehaviour
    {
        [Header("Visuals (optional)")]
        [Tooltip("Optional child GameObject toggled on while the player is touching this anchor.")]
        [SerializeField] private GameObject armedHighlight;

        [Header("Topology")]
        [Tooltip("Anchors that this part can connect to (polygon-edge neighbors). " +
                 "Leave empty to auto-wire to the two nearest sibling DrawParts at scene load.")]
        [SerializeField] private DrawPart[] neighbors;

        public Transform Transform => transform;

        /// <summary>Polygon-edge neighbors (read-only). RailPaintController uses these to pick
        /// which edge the player can slide along from this anchor.</summary>
        public IReadOnlyList<DrawPart> Neighbors => neighbors;

        /// <summary>True if <paramref name="other"/> is in this part's neighbor set.</summary>
        public bool IsNeighborOf(DrawPart other)
        {
            if (other == null || neighbors == null) return false;
            for (int i = 0; i < neighbors.Length; i++)
                if (neighbors[i] == other) return true;
            return false;
        }

        private void Awake()
        {
            SetHighlight(false);
            EnsureNeighborsWired();
        }

        /// <summary>Wires the two nearest sibling anchors as neighbors if none are set yet.
        /// Idempotent — safe to call from EdgeNetwork before it reads <see cref="Neighbors"/>,
        /// so edge construction never depends on Awake ordering.</summary>
        public void EnsureNeighborsWired()
        {
            if (neighbors == null || neighbors.Length == 0) AutoWireNeighbors();
        }

        /// <summary>Turn the highlight on while the player is touching this anchor.</summary>
        public void OnPlayerEntered() => SetHighlight(true);

        /// <summary>Turn the highlight off when the player leaves this anchor.</summary>
        public void OnPlayerExited() => SetHighlight(false);

        private void AutoWireNeighbors()
        {
            var all = ResolveScopedDrawParts();
            if (all.Length < 2) return;

            int myIndex = Array.IndexOf(all, this);
            if (myIndex < 0) return;

            var positions = new Vector3[all.Length];
            for (int i = 0; i < all.Length; i++) positions[i] = all[i].transform.position;

            var graph = DrawPartNeighborGraph.ComputeNearestNeighbors(positions, k: 2);
            var indices = graph[myIndex];
            neighbors = new DrawPart[indices.Length];
            for (int i = 0; i < indices.Length; i++) neighbors[i] = all[indices[i]];
        }

        /// <summary>
        /// In the mega-scene, anchors must only pair with siblings inside the same
        /// level group ("Level_*"); otherwise different levels' spheres (which can
        /// overlap in world space) would wire as neighbors. Walk up to the nearest
        /// "Level_*" ancestor and scope to it. Falls back to a whole-scene scan for
        /// the legacy scene-per-level layout where no such ancestor exists.
        /// </summary>
        private DrawPart[] ResolveScopedDrawParts()
        {
            Transform levelRoot = transform;
            while (levelRoot != null && !levelRoot.name.StartsWith("Level_", StringComparison.Ordinal))
                levelRoot = levelRoot.parent;

            if (levelRoot != null)
                return levelRoot.GetComponentsInChildren<DrawPart>(includeInactive: true);

            return UnityEngine.Object.FindObjectsByType<DrawPart>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void SetHighlight(bool on)
        {
            if (armedHighlight != null && armedHighlight.activeSelf != on)
            {
                armedHighlight.SetActive(on);
            }
        }
    }
}
