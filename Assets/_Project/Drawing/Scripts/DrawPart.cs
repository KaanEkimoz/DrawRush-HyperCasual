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

        private readonly DrawPartStateMachine _fsm = new();

        public bool IsCompleted => _fsm.IsCompleted;
        public Transform Transform => transform;
        public DrawingPhase Phase => _fsm.Phase;

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
