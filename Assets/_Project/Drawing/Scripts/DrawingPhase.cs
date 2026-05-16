namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Explicit phases of a DrawPart's lifecycle. Replaces the four-boolean encoding
    /// (isPlayerEntered + isGoingToPlayer + isReachedToPlayer + isDrawCompleted)
    /// with a single state field — eliminates impossible combinations and makes the
    /// transition table inspectable.
    /// </summary>
    public enum DrawingPhase
    {
        /// <summary>Default state. No interaction has occurred yet.</summary>
        Idle = 0,

        /// <summary>Player intersected this part; trail is being lerped toward the player.</summary>
        Returning = 1,

        /// <summary>Trail caught up to the player; ready for Interact() to attach or connect.</summary>
        Armed = 2,

        /// <summary>Trail has been re-parented to the player and is actively drawing.</summary>
        Drawing = 3,

        /// <summary>Connection finalised; this part is consumed.</summary>
        Done = 4,
    }
}
