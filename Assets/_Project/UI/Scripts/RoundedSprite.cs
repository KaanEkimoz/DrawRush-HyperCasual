using UnityEngine;

namespace DrawRush.UI
{
    /// <summary>
    /// A white rounded-rectangle sprite made in code, 9-sliced so one texture serves every card,
    /// button and pip. Tint it with <c>Image.color</c>. Built once and shared.
    /// </summary>
    public static class RoundedSprite
    {
        private const int Size = 64;
        private const int Radius = 24;
        private static Sprite _sprite;

        public static Sprite Get()
        {
            if (_sprite != null) return _sprite;
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                name = "RoundedRect",
            };
            var px = new Color32[Size * Size];
            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                // Distance outside the inner rectangle; anti-aliased over one pixel.
                float dx = Mathf.Max(Radius - x - 0.5f, x + 0.5f - (Size - Radius), 0f);
                float dy = Mathf.Max(Radius - y - 0.5f, y + 0.5f - (Size - Radius), 0f);
                float a = Mathf.Clamp01(Radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                px[y * Size + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            _sprite = Sprite.Create(tex, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(Radius, Radius, Radius, Radius));
            _sprite.name = "RoundedRect";
            return _sprite;
        }

        /// <summary>Make <paramref name="image"/> a rounded card of <paramref name="color"/>.
        /// <paramref name="roundness"/> scales the corner radius (1 = 24 px at canvas scale).</summary>
        public static void Apply(UnityEngine.UI.Image image, Color color, float roundness = 1f)
        {
            image.sprite = Get();
            image.type = UnityEngine.UI.Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, roundness);
            image.color = color;
        }
    }
}
