using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Gives a corner drop a solid dark rim so it never disappears when its colour happens to match
    /// the wall or ground behind it. The rim is a back-face-only copy of the drop's own mesh, pushed
    /// out along the mesh normals by an <see cref="DropOutline.outlineMaterial"/> inverted-hull
    /// shader — so it stays a perfectly concentric outline from EVERY camera angle (the drops
    /// billboard to the camera, and this follows), never sliding off to one side the way a
    /// position-offset copy did.
    ///
    /// Built at runtime from the drop's own mesh, so it costs nothing in the scene file and picks up
    /// whatever mesh the drop uses.
    /// </summary>
    [DefaultExecutionOrder(45)]
    public sealed class DropOutline : MonoBehaviour
    {
        [Tooltip("Inverted-hull outline material (DrawRush/DropOutline shader). Assign the real " +
                 "DropOutline.mat asset — the Shader.Find fallback only resolves in the editor.")]
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
            // Coincident with the drop: the inverted-hull shader does the growing along normals, so
            // there is NO position offset to make the rim lopsided, and no scale to stretch the tip.
            _outline.localPosition = Vector3.zero;
            _outline.localRotation = Quaternion.identity;
            _outline.localScale = Vector3.one;
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
            var sh = Shader.Find("DrawRush/DropOutline");
            if (sh == null)
            {
                Debug.LogError("DropOutline has no outlineMaterial and DrawRush/DropOutline was " +
                               "stripped — assign DropOutline.mat in the inspector.");
                return null;
            }
            _fallback = new Material(sh);
            return _fallback;
        }
    }
}
