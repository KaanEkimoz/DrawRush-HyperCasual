using UnityEngine;

namespace DrawRush.Enemy
{
    /// <summary>
    /// A chunk the putty eraser tears off itself and lobs at the hero. It flies in an arc to where
    /// the hero stood, a ring on the page marks where it will land, and it hurts only if the hero is
    /// still inside the ring when it lands. It leaves a small splat that fades.
    /// </summary>
    public sealed class PuttyGlob : MonoBehaviour
    {
        private const float GlobSize = 0.5f;
        private const float SplatSeconds = 1.2f;

        private EnemyCombat _owner;
        private Vector3 _from, _to;
        private float _time, _duration, _height, _radius;
        private LineRenderer _ring;
        private static Material _ringMaterial;
        private static Mesh _globMesh;

        // Every UV of the sphere points at one plain body texel of the putty atlas (the grey
        // patch its back uses), so the chunk wears the putty's colour, whichever variant it is.
        private static readonly Vector2 BodyTexel = new(248f / 256f, 32f / 256f);

        private static Mesh GlobMesh(Mesh sphere)
        {
            if (_globMesh != null) return _globMesh;
            _globMesh = Instantiate(sphere);
            _globMesh.name = "PuttyGlob";
            var uv = new Vector2[_globMesh.vertexCount];
            for (int i = 0; i < uv.Length; i++) uv[i] = BodyTexel;
            _globMesh.uv = uv;
            return _globMesh;
        }

        /// <summary>Throw a glob from <paramref name="from"/> to <paramref name="to"/>, looking like <paramref name="look"/>.</summary>
        public static void Launch(EnemyCombat owner, Renderer look, Vector3 from, Vector3 to, float duration, float height, float radius)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "PuttyGlob";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = Vector3.one * GlobSize;
            var filter = go.GetComponent<MeshFilter>();
            filter.sharedMesh = GlobMesh(filter.sharedMesh);
            var r = go.GetComponent<Renderer>();
            if (look != null)
            {
                r.sharedMaterial = look.sharedMaterial;
                var block = new MaterialPropertyBlock();
                look.GetPropertyBlock(block);
                r.SetPropertyBlock(block);
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var glob = go.AddComponent<PuttyGlob>();
            glob._owner = owner;
            glob._from = from;
            glob._to = to;
            glob._duration = Mathf.Max(0.1f, duration);
            glob._height = height;
            glob._radius = radius;
            glob._ring = MakeRing(to, radius);
            go.transform.position = from;
        }

        private static LineRenderer MakeRing(Vector3 at, float radius)
        {
            if (_ringMaterial == null) _ringMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Auto_GlobRing" };
            var line = new GameObject("GlobRing").AddComponent<LineRenderer>();
            line.sharedMaterial = _ringMaterial;
            line.loop = true;
            line.useWorldSpace = true;
            line.widthMultiplier = 0.07f;
            line.startColor = line.endColor = new Color(0.9f, 0.15f, 0.2f, 0.85f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            const int n = 28;
            line.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(at.x + Mathf.Cos(a) * radius, 0.04f, at.z + Mathf.Sin(a) * radius));
            }
            return line;
        }

        private void Update()
        {
            if (_owner == null || !_owner.isActiveAndEnabled) { Finish(false); return; }
            _time += Time.deltaTime;
            float k = Mathf.Clamp01(_time / _duration);
            Vector3 p = Vector3.Lerp(_from, _to, k);
            p.y = Mathf.Lerp(_from.y, _to.y, k) + Mathf.Sin(k * Mathf.PI) * _height;
            transform.position = p;
            transform.Rotate(400f * Time.deltaTime, 250f * Time.deltaTime, 0f, Space.Self);
            if (_ring != null) _ring.widthMultiplier = Mathf.Lerp(0.05f, 0.12f, k);
            if (k >= 1f) Finish(true);
        }

        private void Finish(bool landed)
        {
            if (_ring != null) Destroy(_ring.gameObject);
            if (landed)
            {
                Transform player = Core.GameServices.Player;
                if (player != null)
                {
                    Vector3 d = player.position - _to;
                    d.y = 0f;
                    if (d.magnitude <= _radius) _owner.HitPlayer(player, _to);
                }
                // The chunk stays as a flat splat for a moment.
                transform.position = new Vector3(_to.x, 0.03f, _to.z);
                transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                transform.localScale = new Vector3(GlobSize * 1.6f, 0.06f, GlobSize * 1.6f);
                enabled = false;
                Destroy(gameObject, SplatSeconds);
                return;
            }
            Destroy(gameObject);
        }
    }
}
