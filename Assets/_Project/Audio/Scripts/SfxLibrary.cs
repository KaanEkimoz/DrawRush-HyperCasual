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
        [Tooltip("Coins awarded.")]
        public Cue coin = new();
    }
}
