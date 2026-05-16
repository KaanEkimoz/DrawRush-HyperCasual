using UnityEngine;

namespace Studios208.DrawRush.Utilities
{
    [RequireComponent(typeof(Renderer))]
    public sealed class RandomMaterial : MonoBehaviour
    {
        [SerializeField] private Material[] randomMaterials;

        private void Start()
        {
            if (randomMaterials == null || randomMaterials.Length == 0) return;
            var renderer = GetComponent<Renderer>();
            renderer.material = randomMaterials[Random.Range(0, randomMaterials.Length)];
        }
    }
}
