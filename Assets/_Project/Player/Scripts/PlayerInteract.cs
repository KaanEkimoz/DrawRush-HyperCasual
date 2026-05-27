using System;
using UnityEngine;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Tracks whether the player is inside a DrawArea and which anchor sphere it last
    /// touched. The edge-painting controller (<see cref="RailPaintController"/>) consumes
    /// <see cref="IsInDrawArea"/>, <see cref="CurrentPart"/>, and <see cref="PartTouched"/>
    /// to drive paint movement. No chain / closed-loop logic — edges and their fill state
    /// are owned by EdgeNetwork / DrawEdge, and the persistent paint is rendered by
    /// DrawEdgeView (the player no longer carries a drawing trail, which would double the
    /// edge-fill line).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerInteract : MonoBehaviour
    {
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _isInDrawArea = true;
                return;
            }

            if (!_isInDrawArea) return;

            var part = other.GetComponent<DrawPart>();
            if (part == null) return;

            _currentPart = part;
            part.OnPlayerEntered();        // glow highlight on
            PartTouched?.Invoke(part);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(drawAreaTag))
            {
                _isInDrawArea = false;
                _currentPart = null;
                return;
            }

            var part = other.GetComponent<DrawPart>();
            if (part != null) part.OnPlayerExited();    // glow highlight off
        }

        /// <summary>Clears drawing state. Called by LevelManager when switching levels
        /// in-scene, since there is no scene reload to reset this implicitly.</summary>
        public void ResetChain()
        {
            _currentPart = null;
        }
    }
}
