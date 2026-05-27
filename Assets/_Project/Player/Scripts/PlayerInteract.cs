using System;
using UnityEngine;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Tracks whether the player is inside a DrawArea and which anchor sphere it last
    /// touched, and carries a persistent TrailRenderer that emits while inside the area.
    /// The edge-painting controller (<see cref="RailPaintController"/>) consumes
    /// <see cref="IsInDrawArea"/>, <see cref="CurrentPart"/>, and <see cref="PartTouched"/>
    /// to drive paint movement. No chain / closed-loop logic — edges and their fill state
    /// are owned by EdgeNetwork / DrawEdge.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerInteract : MonoBehaviour
    {
        [Header("Refs"), Space]
        [Tooltip("TrailRenderer that draws the player's path while inside the DrawArea. " +
                 "If empty, auto-resolved from the player hierarchy.")]
        [SerializeField] private TrailRenderer playerTrail;
        [Tooltip("Optional material applied to the player trail if it has none.")]
        [SerializeField] private Material trailMaterial;

        [Header("Tags"), Space]
        [SerializeField] private string drawAreaTag = "DrawArea";

        private DrawPart _currentPart;
        private bool _isInDrawArea;

        /// <summary>True while the player is inside a DrawArea trigger.</summary>
        public bool IsInDrawArea => _isInDrawArea;

        /// <summary>The anchor whose trigger the player most recently entered, or null.</summary>
        public DrawPart CurrentPart => _currentPart;

        /// <summary>Raised whenever the player enters an anchor's trigger while in the draw area.</summary>
        public event Action<DrawPart> PartTouched;

        private void Awake()
        {
            if (playerTrail == null) playerTrail = GetComponentInChildren<TrailRenderer>(includeInactive: true);
            SetTrailEmitting(false);

            if (playerTrail != null && playerTrail.sharedMaterial == null && trailMaterial != null)
            {
                playerTrail.sharedMaterial = trailMaterial;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _isInDrawArea = true;
                SetTrailEmitting(true);
                return;
            }

            if (!_isInDrawArea) return;

            var part = other.GetComponent<DrawPart>();
            if (part == null) return;

            _currentPart = part;
            part.OnPlayerEntered();                       // glow highlight on
            if (playerTrail != null) playerTrail.Clear(); // fresh trail for the next edge
            PartTouched?.Invoke(part);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _isInDrawArea = false;
                _currentPart = null;
                SetTrailEmitting(false);
                return;
            }

            var part = other.GetComponent<DrawPart>();
            if (part != null) part.OnPlayerExited();       // glow highlight off
        }

        /// <summary>Clears drawing state and the trail. Called by LevelManager when switching
        /// levels in-scene, since there is no scene reload to reset this implicitly.</summary>
        public void ResetChain()
        {
            _currentPart = null;
            if (playerTrail != null) playerTrail.Clear();
        }

        private void SetTrailEmitting(bool on)
        {
            if (playerTrail == null) return;
            playerTrail.emitting = on;
            if (!on) playerTrail.Clear();
        }
    }
}
