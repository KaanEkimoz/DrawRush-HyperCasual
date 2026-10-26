using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Enemy;
using TMPro;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// Places one booster pickup at a time on the field: on an unpainted stretch of the shape (so
    /// grabbing it is on the way), away from enemies and from the player. The first one comes a
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

            var enemies = FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Vector3 player = transform.position;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                DrawEdge e = _open[Random.Range(0, _open.Count)];
                float t = RandomUnpaintedT(e);
                if (t < 0f) continue;
                Vector3 p = e.PointAt(t);
                if (Flat(p - player) < Config.MinPlayerDistance) continue;
                bool nearEnemy = false;
                foreach (EnemyFollow en in enemies)
                    if (en.isActiveAndEnabled && Flat(en.transform.position - p) < Config.MinEnemyDistance) { nearEnemy = true; break; }
                if (nearEnemy) continue;

                Spawn(Config.Pick(boosts.RecentlyHit, () => Random.value), p);
                return true;
            }
            return false;
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
