using System;
using System.Collections.Generic;
using UnityEngine;
using DrawRush.Drawing;

namespace DrawRush.Player
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
        // Every anchor trigger the player is standing in. Needed because touches are enter-only:
        // at a corner the next edge's drop sits ~0.4 from the one just finished, so the player is
        // usually already inside it when the edge completes and no new enter ever fires.
        private readonly List<DrawPart> _overlapping = new();

        /// <summary>The anchor whose trigger the player most recently entered, or null.</summary>
        public DrawPart CurrentPart => _currentPart;

        /// <summary>Raised whenever the player enters an anchor's trigger.</summary>
        public event Action<DrawPart> PartTouched;

        private void OnTriggerEnter(Collider other)
        {
            var part = other.GetComponent<DrawPart>();
            if (part == null) return;

            _currentPart = part;
            if (!_overlapping.Contains(part)) _overlapping.Add(part);
            part.OnPlayerEntered();
            PartTouched?.Invoke(part);
        }

        private void OnTriggerExit(Collider other)
        {
            var part = other.GetComponent<DrawPart>();
            if (part == null) return;
            _overlapping.Remove(part);
            part.OnPlayerExited();
        }

        /// <summary>The closest anchor the player is currently standing in that passes
        /// <paramref name="accept"/>. Hidden/disabled drops are skipped (deactivating a trigger
        /// sends no exit, so stale entries are pruned here).</summary>
        public bool TryGetOverlappingPart(Func<DrawPart, bool> accept, out DrawPart part)
        {
            part = null;
            float best = float.PositiveInfinity;
            for (int i = _overlapping.Count - 1; i >= 0; i--)
            {
                DrawPart p = _overlapping[i];
                if (p == null || !p.isActiveAndEnabled) { _overlapping.RemoveAt(i); continue; }
                if (accept != null && !accept(p)) continue;
                float d = (p.Transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; part = p; }
            }
            return part != null;
        }

        /// <summary>Clears drawing state. Called by LevelManager when switching levels
        /// in-scene, since there is no scene reload to reset this implicitly.</summary>
        public void ResetChain()
        {
            _currentPart = null;
            _overlapping.Clear();
        }
    }
}
