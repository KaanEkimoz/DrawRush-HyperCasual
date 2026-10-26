using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Enemy;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Places one booster pickup at a time on the field: beside an unpainted stretch of the shape,
    /// ahead of the player, defensive ones near danger, never on top of an enemy or the player.
    /// In a level with no enemies only speed comes up. The first one comes a
    /// few seconds into a level; after one is taken or expires, the next follows 8-12 s later.
    /// After a recent hit, shield and freeze are likelier (quiet pity, never announced).
    /// </summary>
    public sealed class BoosterSpawner : MonoBehaviour
    {
        [SerializeField] private PlayerBoosts boosts;
        [SerializeField] private FreeDrawPainter painter;
        [Tooltip("Material for pickups (tinted per type through a property block).")]
        [SerializeField] private Material pickupMaterial;
        [SerializeField] private float pickupSize = 0.7f;
        [Tooltip("Font for the name floating over each pickup.")]
        [SerializeField] private TMP_FontAsset labelFont;
        [Tooltip("Material for the soft glowing disc under each pickup (tinted per type).")]
        [SerializeField] private Material glowMaterial;
        [SerializeField] private float pickupHeight = 0.6f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private BoosterPickup _current;
        private float _nextSpawn = float.PositiveInfinity;
        private readonly List<DrawEdge> _open = new();
        private readonly List<Vector3> _enemies = new();
        private Vector3 _lastPos, _heading;

        private BoosterConfig Config => boosts != null ? boosts.Config : null;

        private void Awake()
        {
            if (boosts == null) boosts = GetComponent<PlayerBoosts>();
            if (painter == null) painter = GetComponent<FreeDrawPainter>();
        }

        private void OnEnable() { if (painter != null) painter.LevelBound += OnLevelBound; }
        private void OnDisable() { if (painter != null) painter.LevelBound -= OnLevelBound; }

        private void OnLevelBound(EdgeNetwork _)
        {
            Clear();
            if (Config != null) _nextSpawn = Time.time + Config.FirstSpawnDelay;
        }

        private void Update()
        {
            Vector3 step = transform.position - _lastPos; step.y = 0f;
            if (step.sqrMagnitude > 1e-6f) _heading = Vector3.Lerp(_heading, step.normalized, 0.1f);
            _lastPos = transform.position;

            var state = GameServices.State;
            if (state != null && state.IsGameWon) { Clear(); return; }
            if (_current != null || Config == null || Time.time < _nextSpawn) return;
            if (TrySpawn()) return;
            _nextSpawn = Time.time + 1f;   // no good spot right now; try again shortly
        }

        public void Collected(BoosterPickup pickup)
        {
            if (pickup != _current) return;
            boosts.Grant(pickup.Type);
            Despawn();
        }

        public void Expired(BoosterPickup pickup)
        {
            if (pickup == _current) Despawn();
        }

        private void Despawn()
        {
            if (_current != null) Destroy(_current.gameObject);
            _current = null;
            Vector2 gap = Config.SpawnInterval;
            _nextSpawn = Time.time + Random.Range(gap.x, gap.y);
        }

        private void Clear()
        {
            if (_current != null) Destroy(_current.gameObject);
            _current = null;
            _nextSpawn = float.PositiveInfinity;
        }

        private bool TrySpawn()
        {
            EdgeNetwork net = painter != null ? painter.Network : null;
            if (net == null) return false;
            _open.Clear();
            foreach (DrawEdge e in net.Edges) if (!e.IsComplete) _open.Add(e);
            if (_open.Count == 0) return false;

            _enemies.Clear();
            foreach (EnemyFollow en in FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (en.isActiveAndEnabled) _enemies.Add(en.transform.position);

            // Type first, then a spot that suits it. With no enemies only speed can come up:
            // a shield or a freeze in an empty level is a pickup that does nothing.
            BoosterType type = Config.Pick(boosts.RecentlyHit, () => Random.value, _enemies.Count > 0);
            bool defensive = BoosterConfig.NeedsEnemies(type);
            Vector3 player = transform.position;
            Vector2 pref = Config.PreferredPlayerDistance;

            float bestScore = float.NegativeInfinity;
            Vector3 best = default;
            for (int attempt = 0; attempt < 24; attempt++)
            {
                DrawEdge e = _open[Random.Range(0, _open.Count)];
                float t = RandomUnpaintedT(e);
                if (t < 0f) continue;

                // Beside the line, not on it: walking the shape should not collect it by itself.
                Vector3 onLine = e.PointAt(t);
                Vector3 side = Vector3.Cross(Vector3.up, e.TangentAt(t));
                side.y = 0f;
                Vector3 p = side.sqrMagnitude > 1e-6f
                    ? onLine + side.normalized * (Random.value < 0.5f ? -1f : 1f) * Config.SideOffset
                    : onLine;
                // Walkable? The NavMesh sits below the drawing height, so judge it on the ground plane.
                p = NavMesh.SamplePosition(p, out NavMeshHit hit, 1.5f, NavMesh.AllAreas) && Flat(hit.position - p) < 0.6f
                    ? new Vector3(hit.position.x, p.y, hit.position.z)
                    : onLine;

                float toPlayer = Flat(p - player);
                if (toPlayer < Config.MinPlayerDistance) continue;
                float toEnemy = NearestEnemy(p);
                if (toEnemy < Config.MinEnemyDistance) continue;

                // Ahead on the drawing (4-10 m, the way the player is heading), and for defensive
                // boosters near danger: the research's risk-for-reward rule.
                float score = -(toPlayer < pref.x ? pref.x - toPlayer : toPlayer > pref.y ? toPlayer - pref.y : 0f);
                if (_heading.sqrMagnitude > 1e-4f && Vector3.Dot(p - player, _heading) > 0f) score += 2f;
                if (defensive && toEnemy < float.PositiveInfinity) score -= Mathf.Max(0f, toEnemy - Config.DangerRadius) * 0.5f;
                if (score > bestScore) { bestScore = score; best = p; }
            }
            if (float.IsNegativeInfinity(bestScore)) return false;
            Spawn(type, best);
            return true;
        }

        private float NearestEnemy(Vector3 p)
        {
            float best = float.PositiveInfinity;
            foreach (Vector3 e in _enemies) best = Mathf.Min(best, Flat(e - p));
            return best;
        }

        private static float RandomUnpaintedT(DrawEdge e)
        {
            for (int i = 0; i < 8; i++)
            {
                float t = Random.Range(0.1f, 0.9f);
                bool painted = false;
                foreach (Vector2 s in e.Fill.Spans) if (t >= s.x && t <= s.y) { painted = true; break; }
                if (!painted) return t;
            }
            return -1f;
        }

        private void Spawn(BoosterType type, Vector3 at)
        {
            var root = new GameObject("Booster_" + type);
            root.transform.position = new Vector3(at.x, pickupHeight, at.z);
            var col = root.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = pickupSize * 0.9f;
            var body = root.AddComponent<Rigidbody>();   // kinematic so the player's trigger sees it
            body.isKinematic = true;

            PrimitiveType shape = type switch
            {
                BoosterType.Shield => PrimitiveType.Sphere,
                BoosterType.Freeze => PrimitiveType.Cube,
                BoosterType.Speed => PrimitiveType.Capsule,
                _ => PrimitiveType.Cylinder,
            };
            var visual = GameObject.CreatePrimitive(shape);
            Destroy(visual.GetComponent<Collider>());
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = type == BoosterType.Speed ? new Vector3(0.5f, 0.45f, 0.5f) * pickupSize * 1.4f
                : type == BoosterType.Shockwave ? new Vector3(1f, 0.25f, 1f) * pickupSize
                : Vector3.one * pickupSize;
            if (type == BoosterType.Freeze) visual.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            var r = visual.GetComponent<Renderer>();
            if (pickupMaterial != null) r.sharedMaterial = pickupMaterial;
            var mpb = new MaterialPropertyBlock();
            Color c = Config.ColorOf(type);
            mpb.SetColor(BaseColorId, c);
            mpb.SetColor(ColorId, c);
            r.SetPropertyBlock(mpb);

            // A glowing disc on the ground and the booster's name floating above it, so it reads
            // as "SHIELD" from across the field rather than as an anonymous shape.
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(disc.GetComponent<Collider>());
            disc.name = "Glow";
            disc.transform.SetParent(root.transform, false);
            disc.transform.localPosition = new Vector3(0f, -pickupHeight + 0.03f, 0f);
            disc.transform.localScale = new Vector3(pickupSize * 2.2f, 0.01f, pickupSize * 2.2f);
            var dr = disc.GetComponent<Renderer>();
            if (glowMaterial != null) dr.sharedMaterial = glowMaterial;
            var dmpb = new MaterialPropertyBlock();
            var glow = new Color(c.r, c.g, c.b, 0.45f);
            dmpb.SetColor(BaseColorId, glow);
            dmpb.SetColor(ColorId, glow);
            dr.SetPropertyBlock(dmpb);
            dr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var labelGo = new GameObject("Label", typeof(TextMeshPro));
            labelGo.transform.SetParent(root.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, pickupSize * 1.3f, 0f);
            var label = labelGo.GetComponent<TextMeshPro>();
            if (labelFont != null) label.font = labelFont;
            label.text = Config.TitleOf(type);
            label.fontSize = 4.5f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.fontStyle = FontStyles.Bold;   // no outlineWidth: it instances the material at runtime
            label.rectTransform.sizeDelta = new Vector2(6f, 1.5f);

            _current = root.AddComponent<BoosterPickup>();
            _current.Init(this, type, Config.PickupLifetime, Config.BlinkSeconds);
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }
    }
}
