using System;
using UnityEngine;
using Studios208.DrawRush.Common;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// The minimum surface PlayerInteract / managers need from a draw target.
    /// Hides DrawPart's internal state machine (booleans, trail handling) from callers.
    /// </summary>
    public interface IDrawPart : IInteractable
    {
        event Action<IDrawPart> Completed;
        bool IsCompleted { get; }
        Transform Transform { get; }

        /// <summary>Called by PlayerInteract on first contact (re-arms the part).</summary>
        void OnPlayerEntered();

        /// <summary>Called by PlayerInteract on draw-area exit (cancels armed state).</summary>
        void OnPlayerExited();

        /// <summary>Marks the part as drawn and raises <see cref="Completed"/>. Idempotent.</summary>
        void Complete();
    }
}
