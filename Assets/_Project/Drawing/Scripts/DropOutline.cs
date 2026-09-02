using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// A camera-facing white rim behind a corner drop, so the drop never disappears when its colour
    /// happens to match the wall or ground behind it. The drops are billboarded, so a slightly
    /// larger copy of the same mesh — parented to the drop and pushed a hair away from the camera —
    /// reads as a clean outline from every angle without a custom shader.
    ///
    /// Built at runtime from the drop's own mesh, so it costs nothing in the scene file and picks
    /// up whatever mesh the drop uses.
    /// </summary>
    [DefaultExecutionOrder(45)]
    public sealed class DropOutline : MonoBehaviour
    {
        [Tooltip("Rim size relative to the drop. ~1.15 reads as a clean thin outline.")]
        [SerializeField] private float scale = 1.16f;
        [Tooltip("Push behind the drop along the billboard's away-axis so the drop covers the centre.")]
        [SerializeField] private float backOffset = 0.12f;
        [Tooltip("Outline material — a real asset (URP/Unlit white). Do NOT rely on the Shader.Find " +
                 "fallback, which only resolves in the editor.")]
        [SerializeField] private Material outlineMaterial;

        private Transform _outline;

        private void OnEnable()
        {
            if (_outline != null) { _outline.gameObject.SetActive(true); return; }
            var mf = GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            var go = new GameObject("DropOutline", typeof(MeshFilter), typeof(MeshRenderer));
            _outline = go.transform;
            _outline.SetParent(transform, false);
            _outline.localScale = Vector3.one * scale;
            // Push AWAY from the camera. The billboard leaves local +Z facing the viewer, so the
            // outline goes to -Z or it renders in front and hides the drop.
            _outline.localPosition = new Vector3(0f, 0f, -Mathf.Abs(backOffset));
            go.GetComponent<MeshFilter>().sharedMesh = mf.sharedMesh;

            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = outlineMaterial != null ? outlineMaterial : Fallback();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Material _fallback;
        private static Material Fallback()
        {
            if (_fallback != null) return _fallback;
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null)
            {
                Debug.LogError("DropOutline has no outlineMaterial and URP/Unlit was stripped — " +
                               "assign DropOutline.mat in the inspector.");
                return null;
            }
            _fallback = new Material(sh) { color = Color.white };
            return _fallback;
        }
    }
}
