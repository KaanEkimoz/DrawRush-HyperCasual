using UnityEngine;

namespace DrawRush.Player
{
    /// <summary>
    /// Little arrow floating over the player's head pointing where the rail wants them to go.
    /// It only appears while the rail actually has an opinion — see
    /// <see cref="RailPaintController.TryGetGuidance"/> — so it never nags during free movement,
    /// and it reads the same heading the player's body turns to, rather than deciding its own.
    ///
    /// The arrow lies flat in the XZ plane like the painted trail does: under this game's
    /// third-person camera a horizontal arrow reads as a direction on the ground, where an
    /// upright billboard would just read as a symbol facing the lens.
    ///
    /// Mesh is generated rather than authored (one small chevron, no artist round-trip), but the
    /// MATERIAL is a real project asset on purpose: a runtime Shader.Find resolves fine in the
    /// editor and returns null in a stripped IL2CPP build, which is exactly how every wall in this
    /// project once shipped magenta.
    /// </summary>
    [DefaultExecutionOrder(20)]   // after RailPaintController has refreshed its guidance
    public sealed class RailDirectionArrow : MonoBehaviour
    {
        [Header("Refs (auto-resolved from this GameObject if empty)")]
        [SerializeField] private RailPaintController rail;

        [Tooltip("Material for the arrow. Assign a real asset — do not rely on the Shader.Find " +
                 "fallback, which only works in the editor and leaves the arrow invisible or " +
                 "magenta in a build.")]
        [SerializeField] private Material arrowMaterial;

        [Header("Placement")]
        [Tooltip("Height above the player's origin, in the player's local space (so it follows the " +
                 "character's scale). Enough to clear the pencil the character holds up.")]
        [SerializeField] private float height = 2.7f;
        [Tooltip("Arrow length. Tuned against the ACTUAL game camera, which sits ~14 units back to " +
                 "frame the whole shape — anything smaller is unreadable on a phone even though it " +
                 "looks fine in a close editor view.")]
        [SerializeField] private float size = 1.1f;

        [Header("Motion")]
        [Tooltip("Bob distance, to keep it alive without pulling the eye off the drawing.")]
        [SerializeField] private float bobAmplitude = 0.07f;
        [SerializeField] private float bobSpeed = 3.5f;
        [Tooltip("Degrees per second the arrow swings to a new heading.")]
        [SerializeField] private float turnSpeed = 720f;

        private Transform _arrow;
        private Mesh _mesh;
        private Material _runtimeMaterial;   // only when the fallback had to build one
        private float _bobPhase;

        private void Awake()
        {
            if (rail == null) rail = GetComponent<RailPaintController>();
            BuildArrow();
        }

        private void OnDestroy()
        {
            // Generated mesh + any fallback material are ours; nothing else will collect them.
            if (_mesh != null) Destroy(_mesh);
            if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
        }

        private void LateUpdate()
        {
            if (_arrow == null) return;

            bool show = rail != null && rail.TryGetGuidance(out Vector3 heading) && heading.sqrMagnitude > 0.0001f;
            if (_arrow.gameObject.activeSelf != show) _arrow.gameObject.SetActive(show);
            if (!show) { _bobPhase = 0f; return; }

            _bobPhase += Time.deltaTime * bobSpeed;
            _arrow.localPosition = new Vector3(0f, height + Mathf.Sin(_bobPhase) * bobAmplitude, 0f);

            // World rotation, so the arrow keeps pointing along the rail while the player's body
            // turns underneath it (it is parented to the player and would otherwise inherit).
            rail.TryGetGuidance(out Vector3 dir);
            _arrow.rotation = Quaternion.RotateTowards(
                _arrow.rotation, Quaternion.LookRotation(dir, Vector3.up), turnSpeed * Time.deltaTime);
        }

        private void BuildArrow()
        {
            var go = new GameObject("RailDirectionArrow");
            _arrow = go.transform;
            _arrow.SetParent(transform, false);
            _arrow.localPosition = new Vector3(0f, height, 0f);

            _mesh = BuildMesh(size);
            go.AddComponent<MeshFilter>().sharedMesh = _mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = ResolveMaterial();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            go.SetActive(false);   // nothing to point at until the player is on the rail
        }

        private Material ResolveMaterial()
        {
            if (arrowMaterial != null) return arrowMaterial;

            // Editor-only safety net. A build that reaches this has already lost: URP/Unlit is
            // stripped unless something references it, so Find returns null and the arrow renders
            // magenta or not at all. Shout rather than ship it silently.
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null)
            {
                Debug.LogError("RailDirectionArrow has no arrowMaterial and URP/Unlit could not be " +
                               "found — assign the material asset in the Inspector.", this);
                return null;
            }
            _runtimeMaterial = new Material(unlit) { color = new Color(1f, 0.85f, 0.2f) };
            return _runtimeMaterial;
        }

        // A flat chevron pointing +Z: triangle head on a short shaft. Wound so the visible face
        // points +Y, which is the only side this game's camera ever sees.
        private static Mesh BuildMesh(float s)
        {
            var mesh = new Mesh { name = "RailDirectionArrow" };
            mesh.SetVertices(new[]
            {
                new Vector3(0f,     0f,  1.00f * s),   // 0 tip
                new Vector3(-0.55f * s, 0f, 0.20f * s), // 1
                new Vector3( 0.55f * s, 0f, 0.20f * s), // 2
                new Vector3(-0.22f * s, 0f, 0.20f * s), // 3
                new Vector3( 0.22f * s, 0f, 0.20f * s), // 4
                new Vector3( 0.22f * s, 0f, -0.55f * s),// 5
                new Vector3(-0.22f * s, 0f, -0.55f * s),// 6
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 3, 4, 5, 3, 5, 6 }, 0);
            mesh.SetNormals(new[]
            {
                Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up,
            });
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
