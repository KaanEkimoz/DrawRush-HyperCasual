using UnityEngine;

namespace DrawRush.Audio
{
    /// <summary>
    /// Every sound effect in one asset, so clips and levels tune without touching code or
    /// prefabs — the same ScriptableObject-config pattern as <c>GameConfig</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "SfxLibrary", menuName = "DrawRush/Audio/Sfx Library", order = 0)]
    public sealed class SfxLibrary : ScriptableObject
    {
        /// <summary>A clip plus how loud to play it. A class (not a struct) so the volume
        /// default survives — a new struct field would silently serialize to 0 = silent.</summary>
        [System.Serializable]
        public sealed class Cue
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        [Tooltip("A wall segment starts rising — the game's signature beat.")]
        public Cue wallRise = new();
        [Tooltip("Shape completed / level won.")]
        public Cue win = new();
        [Tooltip("Player ran out of health.")]
        public Cue lose = new();
        [Tooltip("Player took a hit from an enemy.")]
        public Cue hit = new();
        [Tooltip("An enemy is destroyed at the end of a level (win or lose).")]
        public Cue enemyDie = new();
        [Tooltip("Coins awarded.")]
        public Cue coin = new();
        [Tooltip("A UI button was pressed.")]
        public Cue uiClick = new();
        [Tooltip("A booster was picked up.")]
        public Cue boosterPickup = new();
        [Tooltip("The shield absorbed a hit and broke.")]
        public Cue shieldBreak = new();
        [Tooltip("A Dasher starts flashing before its lunge (the audible half of the telegraph).")]
        public Cue dashWindUp = new();
        [Tooltip("Fresh paint was wiped (a hit or an Eater).")]
        public Cue paintLost = new();
    }
}
