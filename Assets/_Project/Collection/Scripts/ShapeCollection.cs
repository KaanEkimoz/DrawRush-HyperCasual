using System;
using UnityEngine;

namespace DrawRush.Collection
{
    /// <summary>
    /// The collectible side of the levels: every level's shape has a 2D illustration and a name, and
    /// clearing the level adds it to the player's collection. Which shapes are collected is not
    /// stored separately — a level with a best star rating has been cleared (PlayerProgress), so
    /// players who cleared levels before this existed already own those entries.
    /// Entries are keyed by level index (the mega-scene's child order), matching that record.
    /// </summary>
    [CreateAssetMenu(menuName = "DrawRush/Shape Collection", fileName = "ShapeCollection")]
    public sealed class ShapeCollection : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private int levelIndex;
            [SerializeField] private string displayName;
            [SerializeField] private Sprite illustration;

            public int LevelIndex => levelIndex;
            public string DisplayName => displayName;
            public Sprite Illustration => illustration;

            public Entry(int levelIndex, string displayName, Sprite illustration)
            {
                this.levelIndex = levelIndex;
                this.displayName = displayName;
                this.illustration = illustration;
            }
        }

        [SerializeField] private Entry[] entries = Array.Empty<Entry>();

        public int Count => entries.Length;
        public Entry Get(int i) => entries[i];

        /// <summary>The entry for the level at <paramref name="levelIndex"/>, or null.</summary>
        public Entry ForLevel(int levelIndex)
        {
            for (int i = 0; i < entries.Length; i++)
                if (entries[i] != null && entries[i].LevelIndex == levelIndex) return entries[i];
            return null;
        }

#if UNITY_EDITOR
        /// <summary>Editor authoring hook (the illustration importer fills the table).</summary>
        public void SetEntries(Entry[] value) => entries = value ?? Array.Empty<Entry>();
#endif
    }
}
