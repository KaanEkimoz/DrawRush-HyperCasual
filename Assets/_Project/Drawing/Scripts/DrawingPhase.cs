namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Explicit phases of a DrawPart's lifecycle in the chain-anchor model.
    /// The legacy Returning phase (trail catching up to the player) was removed
    /// when DrawParts stopped owning their own trail — the player now carries a
    /// persistent TrailRenderer, parts are just anchors.
    /// </summary>
    public enum DrawingPhase
    {
        /// <summary>Default state. Has not been touched yet.</summary>
        Idle = 0,

        /// <summary>Player has touched this part — it is now the active anchor in the chain.</summary>
        Armed = 1,

        /// <summary>A connecting line has been spawned to the next anchor; this anchor will Complete shortly.</summary>
        Drawing = 2,

        /// <summary>Anchor has been visited and the chain has moved on. Idempotent.</summary>
        Done = 3,
    }
}
