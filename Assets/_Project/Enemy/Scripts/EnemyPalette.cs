using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// Colour variants for one enemy look: the same character in other body colours, swapped in
    /// through a property block so the shared material stays untouched. A level full of the same
    /// enemy no longer reads as one blue crowd.
    /// </summary>
    public sealed class EnemyPalette : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [Tooltip("Body textures to pick from, besides the material's own one.")]
        [SerializeField] private Texture2D[] skins = new Texture2D[0];

        private Renderer[] _renderers;
        private MaterialPropertyBlock _mpb;

        /// <summary>Number of extra colours (the material's own one not counted).</summary>
        public int Count => skins.Length;

        /// <summary>Wear the material's own texture or one of <see cref="skins"/>, at random.</summary>
        public void ApplyRandom() => Apply(Random.Range(-1, skins.Length));

        /// <summary>-1 = the material's own texture.</summary>
        public void Apply(int index)
        {
            _renderers ??= GetComponentsInChildren<Renderer>(true);
            _mpb ??= new MaterialPropertyBlock();
            foreach (Renderer r in _renderers)
            {
                if (r == null || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(BaseMapId)) continue;
                Texture tex = index >= 0 && index < skins.Length && skins[index] != null ? skins[index] : r.sharedMaterial.GetTexture(BaseMapId);
                r.GetPropertyBlock(_mpb);
                _mpb.SetTexture(BaseMapId, tex);
                r.SetPropertyBlock(_mpb);
            }
        }

#if UNITY_EDITOR
        public void SetSkins(Texture2D[] value) => skins = value;
#endif
    }
}
