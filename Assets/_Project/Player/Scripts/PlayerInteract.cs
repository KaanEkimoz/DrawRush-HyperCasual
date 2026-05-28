using System;
using UnityEngine;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Tracks which anchor sphere the player most recently touched and raises
    /// <see cref="PartTouched"/> for the edge-painting controller. The DrawArea
    /// gating was removed — anchor triggers are honored everywhere.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class PlayerInteract : MonoBehaviour
    {
        private DrawPart _currentPart;

        /// <summary>The anchor whose trigger the player most recently entered, or null.</summary>
        public DrawPart CurrentPart => _currentPart;

        /// <summary>Raised whenever the player enters an anchor's trigger.</summary>
        public event Action<DrawPart> PartTouched;

        private void OnTriggerEnter(Collider other)
        {
            var part = other.GetComponent<DrawPart>();
            if (part == null) return;

            _currentPart = part;
            part.OnPlayerEntered();
            PartTouched?.Invoke(part);
        }

        private void OnTriggerExit(Collider other)
        {
            var part = other.GetComponent<DrawPart>();
            if (part != null) part.OnPlayerExited();
        }

        /// <summary>Clears drawing state. Called by LevelManager when switching levels
        /// in-scene, since there is no scene reload to reset this implicitly.</summary>
        public void ResetChain()
        {
            _currentPart = null;
        }
    }
}
