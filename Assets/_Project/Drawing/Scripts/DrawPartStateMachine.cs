using System;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Tiny whitelist-driven state machine for <see cref="DrawingPhase"/>. Pure C#,
    /// no Unity types — fully unit-testable. Chain-anchor model (post 2026-05-16):
    /// Idle → Armed → Drawing → Done. Exit back to Idle is allowed from Armed/Drawing
    /// to support cancel-on-DrawArea-exit gameplay rules.
    /// </summary>
    public sealed class DrawPartStateMachine
    {
        /// <summary>Fired when a transition succeeds. Args: (from, to).</summary>
        public event Action<DrawingPhase, DrawingPhase> Transitioned;

        public DrawingPhase Phase { get; private set; } = DrawingPhase.Idle;

        public bool IsCompleted => Phase == DrawingPhase.Done;

        /// <summary>Attempts to move to <paramref name="next"/>. Returns true if the
        /// transition is whitelisted from the current phase; emits Transitioned on success.</summary>
        public bool TryTransition(DrawingPhase next)
        {
            if (!CanTransition(Phase, next)) return false;
            var prev = Phase;
            Phase = next;
            Transitioned?.Invoke(prev, next);
            return true;
        }

        /// <summary>Resets the machine to Idle without firing Transitioned.</summary>
        public void ResetToIdle()
        {
            Phase = DrawingPhase.Idle;
        }

        /// <summary>Pure whitelist function — exposed for tests.</summary>
        public static bool CanTransition(DrawingPhase from, DrawingPhase to)
        {
            if (from == to) return false;
            if (from == DrawingPhase.Done) return false;

            return from switch
            {
                DrawingPhase.Idle    => to is DrawingPhase.Armed,
                DrawingPhase.Armed   => to is DrawingPhase.Drawing or DrawingPhase.Done or DrawingPhase.Idle,
                DrawingPhase.Drawing => to is DrawingPhase.Done or DrawingPhase.Idle,
                _ => false,
            };
        }
    }
}
