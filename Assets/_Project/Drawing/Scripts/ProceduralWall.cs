using UnityEngine;
using UnityEngine.AI;

namespace Studios208.DrawRush.Drawing
{
    /// <summary>
    /// Builds a wall mesh extruded along an edge's geometry (straight OR arc) and raises it from
    /// below ground when the edge is painted. Replaces the old hand-built cube-strip walls:
    /// one modular system that fits any shape, any length, straight or curved, with no per-level
    /// authoring. Lives on the Kenar prefab root; owns a child "WallMesh" GameObject carrying the
    /// MeshFilter/MeshRenderer/NavMeshObstacle. Purely visual + a NavMesh blocker — nothing else
    /// depends on it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProceduralWall : MonoBehaviour
    {
        [Tooltip("Cross-segments used to extrude a curved (arc) wall; straight walls use 1.")]
        [SerializeField] private int arcSegments = 24;
        [Tooltip("Seconds the wall takes to rise from below ground.")]
        [SerializeField] private float riseSeconds = 0.5f;
        [Tooltip("World Y of the wall base (ground level).")]
        [SerializeField] private float baseY = 0f;

        private Transform _wall;
        private MeshFilter _mf;
        private MeshRenderer _mr;
        private NavMeshObstacle _obstacle;
        private Mesh _mesh;
        private MaterialPropertyBlock _mpb;

        private float _height;
        private float _thickness;
        private Vector3 _shownLocal;
        private Vector3 _hiddenLocal;
        private bool _revealed;

        public float Height => _height;
        public float Thickness => _thickness;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        /// <summary>(Re)build the wall mesh along <paramref name="edge"/> and place it hidden
        /// below ground, ready to rise.</summary>
        public void Build(DrawEdge edge, float height, float thickness, Material mat, Color color)
        {
            EnsureChild();
            _height = Mathf.Max(0.01f, height);
            _thickness = Mathf.Max(0.01f, thickness);

            GenerateMesh(edge);

            // Material + color (MaterialPropertyBlock so we don't mutate the shared material).
            if (mat != null) _mr.sharedMaterial = mat;
            else if (_mr.sharedMaterial == null)
                _mr.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _mpb ??= new MaterialPropertyBlock();
            _mr.GetPropertyBlock(_mpb);
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetColor(ColorId, color);
            _mr.SetPropertyBlock(_mpb);

            // NavMesh blocker (runtime-revealed → carve, since the surface is baked at edit time).
            if (_obstacle != null && _mesh != null)
            {
                _obstacle.shape = NavMeshObstacleShape.Box;
                _obstacle.carving = true;
                _obstacle.carveOnlyStationary = true;
                _obstacle.center = _mesh.bounds.center;
                _obstacle.size = _mesh.bounds.size;
            }

            // Rise setup: rest pose = base on ground (local 0); hidden = sunk below ground.
            _shownLocal = Vector3.zero;
            float worldDepth = _height + 0.3f;
            float localDepth = worldDepth / Mathf.Max(1e-4f, transform.lossyScale.y);
            _hiddenLocal = new Vector3(0f, -localDepth, 0f);
            _wall.localPosition = _hiddenLocal;
            _wall.gameObject.SetActive(true);
            _revealed = false;
        }

        /// <summary>Rise into view. Idempotent.</summary>
        public void Reveal()
        {
            if (_revealed || _wall == null) return;
            _revealed = true;
            _ = RiseAsync();
        }

        /// <summary>Hide the wall instantly (level (re)activation / restart).</summary>
        public void HideImmediate()
        {
            _revealed = false;
            if (_wall != null) _wall.gameObject.SetActive(false);
        }

        /// <summary>World bounds of the built wall (for corner sizing). False before Build.</summary>
        public bool TryGetWorldBounds(out Bounds bounds)
        {
            bounds = default;
            if (_mr == null || _mesh == null) return false;
            bounds = _mr.bounds;
            return true;
        }

        private async Awaitable RiseAsync()
        {
            float t = 0f;
            try
            {
                while (t < riseSeconds)
                {
                    t += Time.deltaTime;
                    _wall.localPosition = Vector3.LerpUnclamped(
                        _hiddenLocal, _shownLocal, Mathf.SmoothStep(0f, 1f, t / riseSeconds));
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                }
            }
            catch (System.OperationCanceledException) { return; }
            _wall.localPosition = _shownLocal;
        }

        private void EnsureChild()
        {
            if (_wall != null) return;
            var go = new GameObject("WallMesh");
            go.transform.SetParent(transform, worldPositionStays: false);
            _wall = go.transform;
            _mf = go.AddComponent<MeshFilter>();
            _mr = go.AddComponent<MeshRenderer>();
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            _obstacle = go.AddComponent<NavMeshObstacle>();
            _mesh = new Mesh { name = "ProceduralWall", hideFlags = HideFlags.DontSave };
            _mf.sharedMesh = _mesh;
            go.SetActive(false);
        }

        // Extrude a box cross-section (thickness × height) along the edge centerline. Vertices are
        // baked in this transform's LOCAL space (InverseTransformPoint) so the wall sits at the
        // right world spot under any parent transform; the rise animates localPosition.
        private void GenerateMesh(DrawEdge edge)
        {
            int n = edge.IsArc ? Mathf.Max(2, arcSegments) : 1;
            int rings = n + 1;
            float half = _thickness * 0.5f;

            var verts = new Vector3[rings * 4];
            for (int i = 0; i < rings; i++)
            {
                float t = (float)i / n;
                Vector3 p = edge.PointAt(t);
                p.y = baseY;
                Vector3 tan = edge.TangentAt(t);
                Vector3 side = Vector3.Cross(Vector3.up, tan);
                side = side.sqrMagnitude > 1e-8f ? side.normalized : Vector3.right;

                Vector3 bl = p - side * half;
                Vector3 br = p + side * half;
                Vector3 tl = bl + Vector3.up * _height;
                Vector3 tr = br + Vector3.up * _height;

                int b = i * 4;
                verts[b + 0] = transform.InverseTransformPoint(bl);
                verts[b + 1] = transform.InverseTransformPoint(br);
                verts[b + 2] = transform.InverseTransformPoint(tl);
                verts[b + 3] = transform.InverseTransformPoint(tr);
            }

            var tris = new System.Collections.Generic.List<int>(n * 18 + 12);
            for (int i = 0; i < n; i++)
            {
                int a = i * 4;
                int c = (i + 1) * 4;
                // Left face (bl,tl outward -side): bl_a, tl_a, tl_c, bl_c
                AddQuad(tris, a + 0, a + 2, c + 2, c + 0);
                // Right face (outward +side): br_a, br_c, tr_c, tr_a
                AddQuad(tris, a + 1, c + 1, c + 3, a + 3);
                // Top face (outward +up): tl_a, tr_a, tr_c, tl_c
                AddQuad(tris, a + 2, a + 3, c + 3, c + 2);
            }
            // End caps.
            int last = n * 4;
            AddQuad(tris, 0, 1, 3, 2);                       // start cap (faces back, -tangent)
            AddQuad(tris, last + 1, last + 0, last + 2, last + 3); // end cap (faces +tangent)

            _mesh.Clear();
            _mesh.SetVertices(verts);
            _mesh.SetTriangles(tris, 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
        }

        // Two triangles for a quad (v0,v1,v2,v3) wound consistently.
        private static void AddQuad(System.Collections.Generic.List<int> tris, int v0, int v1, int v2, int v3)
        {
            tris.Add(v0); tris.Add(v1); tris.Add(v2);
            tris.Add(v0); tris.Add(v2); tris.Add(v3);
        }
    }
}
