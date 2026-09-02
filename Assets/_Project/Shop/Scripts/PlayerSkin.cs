using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Shop
{
    /// <summary>
    /// Paints the player the colour of whatever skin is equipped — the character body and the draw
    /// trail together, so a bought skin reads at a glance. Applied through MaterialPropertyBlock so
    /// the shared materials are never instanced (the body material is used by one renderer, but the
    /// principle keeps enemies and future shared users safe).
    ///
    /// Lives on the persistent player, so it survives every level switch and just re-applies when
    /// the equipped skin changes.
    /// </summary>
    [DefaultExecutionOrder(30)]
    public sealed class PlayerSkin : MonoBehaviour
    {
        [SerializeField] private CosmeticLibrary library;
        [Tooltip("Character body renderer (the stickman). Auto-found in children if empty.")]
        [SerializeField] private Renderer body;
        [Tooltip("Draw trail. Auto-found in children if empty.")]
        [SerializeField] private TrailRenderer trail;

        private MaterialPropertyBlock _block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            if (body == null)
            {
                var smr = GetComponentInChildren<SkinnedMeshRenderer>(true);
                body = smr;
            }
            if (trail == null) trail = GetComponentInChildren<TrailRenderer>(true);
        }

        private void OnEnable()
        {
            PlayerProgress.CosmeticChanged += OnCosmeticChanged;
            Apply();
        }

        private void OnDisable()
        {
            PlayerProgress.CosmeticChanged -= OnCosmeticChanged;
        }

        private void OnCosmeticChanged(string _) => Apply();

        private void Apply()
        {
            if (library == null) return;
            string id = PlayerProgress.EquippedCosmetic;
            if (string.IsNullOrEmpty(id)) id = library.DefaultId;
            var item = library.Find(id);
            if (item == null) return;
            Color c = item.color;

            if (body != null)
            {
                _block ??= new MaterialPropertyBlock();
                body.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);   // TCP2 Hybrid body reads _BaseColor for its albedo
                body.SetPropertyBlock(_block);
            }
            if (trail != null)
            {
                // The trail's own start/end colour is what actually tints a URP Unlit line; the
                // material stays shared.
                trail.startColor = c;
                trail.endColor = new Color(c.r, c.g, c.b, 0f);   // fade to transparent along its length
            }
        }
    }
}
