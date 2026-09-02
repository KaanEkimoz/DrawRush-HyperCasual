using System;
using UnityEngine;

namespace DrawRush.Shop
{
    /// <summary>
    /// One buyable colour skin: it recolours the player (and their trail) so a coin balance finally
    /// buys something visible. Kept as plain data — the price and colour are the whole item.
    /// </summary>
    [Serializable]
    public sealed class CosmeticItem
    {
        [Tooltip("Stable id saved to PlayerPrefs. Never rename once shipped, or owners lose the item.")]
        public string id = "default";
        public string displayName = "Default";
        [Tooltip("Coin cost. 0 = owned from the start (the default skin).")]
        public int price = 0;
        [Tooltip("Colour applied to the character body and the draw trail.")]
        public Color color = new Color(0f, 0.286f, 1f, 1f);
    }

    /// <summary>
    /// The catalogue the shop reads. One asset, referenced by the shop UI and the player's skin
    /// applier, so both see the same items and the same ids. The first item is the free default.
    /// </summary>
    [CreateAssetMenu(fileName = "CosmeticLibrary", menuName = "DrawRush/Shop/Cosmetic Library", order = 0)]
    public sealed class CosmeticLibrary : ScriptableObject
    {
        [SerializeField] private CosmeticItem[] items;

        public int Count => items != null ? items.Length : 0;
        public CosmeticItem Get(int i) => items != null && i >= 0 && i < items.Length ? items[i] : null;

        /// <summary>The item with this id, or the first item (the default) if the id is unknown —
        /// so a saved id that no longer exists degrades to the default instead of leaving no skin.</summary>
        public CosmeticItem Find(string id)
        {
            if (items == null || items.Length == 0) return null;
            foreach (var it in items) if (it != null && it.id == id) return it;
            return items[0];
        }

        /// <summary>Id of the default (free) skin — the first item.</summary>
        public string DefaultId => items != null && items.Length > 0 && items[0] != null ? items[0].id : "default";
    }
}
