using System;
using UnityEngine;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// A single anchor in the chain-drawing puzzle. The player walks to each anchor in
    /// sequence and a connecting line is spawned by <c>PlayerInteract</c> between consecutive
    /// anchors. The part no longer owns a trail prefab — the player carries a persistent
    /// TrailRenderer that does the actual drawing. DrawPart's only runtime job is to expose
    /// an interaction surface, track its lifecycle phase, and fire <see cref="Completed"/>
    /// when the chain has moved past it.
    ///
    /// Optional visual feedback: assign <see cref="armedHighlight"/> to a child GameObject
    /// (glow / ring) that the part should toggle on when it becomes the active anchor.
    /// </summary>
    public sealed class DrawPart : MonoBehaviour, IDrawPart
    {
        public event Action<IDrawPart> Completed;

        [Header("Visuals (optional)")]
        [Tooltip("Optional child GameObject toggled on while this part is the active anchor.")]
        [SerializeField] private GameObject armedHighlight;

        [Header("Chain topology")]
        [Tooltip("Anchors that this part can connect to (polygon-edge neighbors). " +
                 "Leave empty to auto-wire to the two nearest sibling DrawParts at scene load.")]
        [SerializeField] private DrawPart[] neighbors;

        private readonly DrawPartStateMachine _fsm = new();

        public bool IsCompleted => _fsm.IsCompleted;
        public Transform Transform => transform;
        public DrawingPhase Phase => _fsm.Phase;

        /// <summary>Polygon-edge neighbors (read-only). RailDrawController uses these
        /// to pick which edge the player can slide along from this anchor.</summary>
        public System.Collections.Generic.IReadOnlyList<DrawPart> Neighbors => neighbors;

        /// <summary>True if <paramref name="other"/> is in this part's neighbor set.</summary>
        public bool IsNeighborOf(DrawPart other)
        {
            if (other == null || neighbors == null) return false;
            for (int i = 0; i < neighbors.Length; i++)
                if (neighbors[i] == other) return true;
            return false;
        }

        [Obsolete("Use OnPlayerEntered / OnPlayerExited instead. Retained for prefab/scene backwards-compat only.")]
        public bool isPlayerEntered
        {
            get => _fsm.Phase is DrawingPhase.Armed or DrawingPhase.Drawing;
            set { /* intentional no-op; encapsulation enforcement */ }
        }

        private void Awake()
        {
            _fsm.ResetToIdle();
            SetHighlight(false);
            if (neighbors == null || neighbors.Length == 0) AutoWireNeighbors();
        }

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

        /// <inheritdoc />
        public void Interact()
        {
            // Chain step. Idle/Done → Armed becomes the active anchor.
            if (_fsm.Phase == DrawingPhase.Idle)
            {
                if (_fsm.TryTransition(DrawingPhase.Armed)) SetHighlight(true);
            }
        }

        /// <inheritdoc />
        public void OnPlayerEntered()
        {
            if (_fsm.Phase == DrawingPhase.Idle && _fsm.TryTransition(DrawingPhase.Armed))
            {
                SetHighlight(true);
            }
        }

        /// <inheritdoc />
        public void OnPlayerExited()
        {
            // Chain-preserving: only un-arm if we never advanced past the visit.
            if (_fsm.Phase == DrawingPhase.Armed)
            {
                _fsm.TryTransition(DrawingPhase.Idle);
                SetHighlight(false);
            }
        }

        /// <inheritdoc />
        public void Complete()
        {
            if (_fsm.IsCompleted) return;

            // Move from Armed (anchor) or Drawing (chain mid-step) to Done.
            if (_fsm.Phase == DrawingPhase.Armed)
            {
                _fsm.TryTransition(DrawingPhase.Drawing);
            }
            _fsm.TryTransition(DrawingPhase.Done);
            SetHighlight(false);
            Completed?.Invoke(this);
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
