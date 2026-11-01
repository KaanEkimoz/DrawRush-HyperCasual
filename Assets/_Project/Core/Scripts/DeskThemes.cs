using UnityEngine;

namespace DrawRush.Core
{
    /// <summary>
    /// Changes the desk from level to level: each one gets the next paper in the list, laid onto
    /// the level's own ground through a property block, so the ground keeps its material and its
    /// tiling and the levels need no edits.
    /// </summary>
    public sealed class DeskThemes : MonoBehaviour
    {
        [Tooltip("Papers in the order levels use them. Keep them light so paint stays readable.")]
        [SerializeField] private Texture2D[] papers;
        [SerializeField] private string groundPath = "Environment/Ground";

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private MaterialPropertyBlock _block;

        /// <summary>Lay the paper for level number <paramref name="levelNumber"/> on
        /// <paramref name="level"/>.</summary>
        public void Apply(Transform level, int levelNumber)
        {
            if (papers == null || papers.Length == 0 || level == null) return;
            Transform ground = level.Find(groundPath);
            if (ground == null || !ground.TryGetComponent(out MeshRenderer renderer)) return;
            _block ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_block);
            _block.SetTexture(BaseMapId, papers[PaperIndex(levelNumber, papers.Length)]);
            renderer.SetPropertyBlock(_block);
        }

        /// <summary>Which of <paramref name="count"/> papers a level number gets.</summary>
        public static int PaperIndex(int levelNumber, int count) =>
            count > 0 ? Mathf.Max(0, levelNumber) % count : 0;
    }
}
