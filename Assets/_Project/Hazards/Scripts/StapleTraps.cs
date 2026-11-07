using System.Collections.Generic;
using DrawRush.Boosters;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DrawRush.Hazards
{
    /// <summary>
    /// Staple traps: rows of staples that pop out of the paper right on the shape's line, on a
    /// loop. While they are up the pencil cannot draw through them and touching them hurts, so a
    /// long straight edge is no longer one easy run: wait for them to sink or come back later.
    /// Read at a glance, in the same language as the putty's target ring: a red ring always marks
    /// the trap, and the disc inside it fills up while the staples are down; full = they are
    /// coming (the ring flashes), solid red = they are up. When the
    /// edge under a trap is finished the trap sinks for good (the wall rises there).
    /// Placed fresh each level from a set level on, more of them later in the campaign.
    /// </summary>
    public sealed class StapleTraps : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerKnockback knockback;
        [SerializeField] private BoosterHud hud;
        [Tooltip("Staple metal. A direct reference keeps the shader in the build.")]
        [SerializeField] private Material metal;

        [Header("Campaign")]
        [Tooltip("First campaign level (1-based) with a trap.")]
        [SerializeField] private int fromLevel = 6;
        [Tooltip("Levels from which a second and a third trap are placed.")]
        [SerializeField] private int twoFrom = 14;
        [SerializeField] private int threeFrom = 26;
        [Tooltip("Edges shorter than this (m) never get a trap: no room to go around it.")]
        [SerializeField] private float minEdgeMeters = 3f;
        [Tooltip("No trap starts closer than this (m) to the hero.")]
        [SerializeField] private float clearOfHero = 2.5f;

        [Header("Cycle (seconds)")]
        [SerializeField] private float downSeconds = 2.4f;
        [SerializeField] private float warnSeconds = 0.7f;
        [SerializeField] private float upSeconds = 1.8f;
        [SerializeField] private float moveSeconds = 0.18f;

        [Header("Shape")]
        [Tooltip("Ground radius (m) that blocks paint and hurts while up.")]
        [SerializeField] private float radius = 0.65f;
        [SerializeField] private float height = 0.55f;
        [SerializeField] private float knockbackForce = 7f;

        private readonly List<Trap> _traps = new();
        private EdgeNetwork _pending;   // bound, not placed yet: waits for the countdown to end
        private Sprite _slitSprite, _discSprite;
        private static Material _ringMaterial;

        private sealed class Trap
        {
            public DrawEdge Edge;
            public Vector3 At;
            public GameObject Root;
            public Transform Staples;
            public SpriteRenderer Slit;
            public SpriteRenderer Timer;
            public LineRenderer Ring;
            public float Clock;
            public int Blocker;   // 0 = not blocking
            public bool Retired;
        }

        /// <summary>How many traps a campaign level places (before the edge limit).</summary>
        public static int TrapCount(int level, int from, int two, int three)
            => level < from ? 0 : level < two ? 1 : level < three ? 2 : 3;

        private void Awake()
        {
            if (painter == null) painter = GetComponent<FreeDrawPainter>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (knockback == null) knockback = GetComponent<PlayerKnockback>();
        }

        private void OnEnable() { if (painter != null) painter.LevelBound += OnLevelBound; }

        private void OnDisable()
        {
            if (painter != null) painter.LevelBound -= OnLevelBound;
            Clear();
        }

        private void OnLevelBound(EdgeNetwork network)
        {
            Clear();
            _pending = network;
        }

        // Placed once the level runs (the countdown freezes time), so the hero is on its spawn
        // point and no trap lands right next to it.
        private void PlacePending()
        {
            EdgeNetwork network = _pending;
            _pending = null;
            if (network == null || !network.isActiveAndEnabled) return;
            int level = PlayerProgress.LevelsPlayed;
            int count = TrapCount(level, fromLevel, twoFrom, threeFrom);
            if (count <= 0) return;
            Place(network, count);
            if (_traps.Count > 0 && level == fromLevel && hud != null)
                hud.Toast("NEW: STAPLE TRAP", "When the red ring fills, staples pop up", new Color(0.9f, 0.25f, 0.2f));
        }

        private void Place(EdgeNetwork network, int count)
        {
            // Straight edges first (the easy runs), longest first; one trap per edge.
            var edges = new List<DrawEdge>();
            foreach (DrawEdge e in network.Edges) if (e.Length >= minEdgeMeters) edges.Add(e);
            edges.Sort((a, b) =>
            {
                int arc = a.IsArc.CompareTo(b.IsArc);
                return arc != 0 ? arc : b.Length.CompareTo(a.Length);
            });
            Vector3 hero = transform.position;
            // The drops along each edge are where the eye goes; a trap sits between them.
            var drops = new List<Vector3>();
            Transform level = network.transform.parent != null ? network.transform.parent : network.transform;
            foreach (Transform c in level.GetComponentsInChildren<Transform>())
                if (c.name == "EdgeDrop") drops.Add(c.position);
            foreach (DrawEdge e in edges)
            {
                if (_traps.Count >= count) break;
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    Vector3 at = e.PointAt(Random.Range(0.25f, 0.75f));
                    at.y = 0f;
                    if (Flat(at - hero) < clearOfHero) continue;
                    bool onDrop = false;
                    foreach (Vector3 d in drops) if (Flat(at - d) < radius + 0.45f) { onDrop = true; break; }
                    if (onDrop) continue;
                    bool crowded = false;
                    foreach (Trap t in _traps) if (Flat(at - t.At) < 3f) { crowded = true; break; }
                    if (crowded) continue;
                    _traps.Add(Build(e, at, network.transform));
                    break;
                }
            }
        }

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        private Trap Build(DrawEdge edge, Vector3 at, Transform parent)
        {
            float t = edge.ClosestT(at, out _);
            Vector3 along = edge.TangentAt(t); along.y = 0f;
            if (along.sqrMagnitude < 1e-6f) along = Vector3.forward;
            Quaternion facing = Quaternion.LookRotation(along.normalized, Vector3.up);

            var root = new GameObject("StapleTrap");
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(at, facing);

            // The slit in the paper the staples come out of.
            var slit = new GameObject("Slit").AddComponent<SpriteRenderer>();
            slit.sprite = SlitSprite();
            slit.sortingOrder = 0;   // hazards draw over the drawing (see GroundLayers)
            slit.transform.SetParent(root.transform, false);
            slit.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            slit.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            slit.transform.localScale = new Vector3(radius * 1.5f, radius * 2.1f, 1f);

            // The timer disc, filling while the staples are down, and the ring around it.
            var timer = new GameObject("Timer").AddComponent<SpriteRenderer>();
            timer.sprite = DiscSprite();
            timer.transform.SetParent(root.transform, false);
            timer.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            timer.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            LineRenderer ring = MakeRing(root.transform);

            // Three staples in a row along the line, each one straddling it.
            var staples = new GameObject("Staples").transform;
            staples.SetParent(root.transform, false);
            for (int i = -1; i <= 1; i++)
            {
                float z = i * radius * 0.55f;
                float w = radius * 1.15f, leg = height, bar = 0.1f;
                Piece(staples, new Vector3(-w * 0.5f, leg * 0.5f, z), new Vector3(bar, leg, bar));
                Piece(staples, new Vector3(w * 0.5f, leg * 0.5f, z), new Vector3(bar, leg, bar));
                Piece(staples, new Vector3(0f, leg, z), new Vector3(w + bar, bar, bar));
            }
            staples.localPosition = new Vector3(0f, -height - 0.1f, 0f);
            staples.gameObject.SetActive(false);

            var trap = new Trap
            {
                Edge = edge, At = at, Root = root, Staples = staples, Slit = slit, Timer = timer, Ring = ring,
                Clock = Random.Range(0f, downSeconds),   // traps do not all rise together
            };
            edge.Completed += _ => Retire(trap);
            return trap;
        }

        private LineRenderer MakeRing(Transform parent)
        {
            if (_ringMaterial == null) _ringMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Auto_TrapRing" };
            var line = new GameObject("Ring").AddComponent<LineRenderer>();
            line.sharedMaterial = _ringMaterial;
            line.transform.SetParent(parent, false);
            line.useWorldSpace = false;
            line.loop = true;
            line.alignment = LineAlignment.TransformZ;
            line.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            line.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            const int n = 40;
            line.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = i / (float)n * Mathf.PI * 2f;
                line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
            }
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }

        private void Piece(Transform parent, Vector3 pos, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(go.GetComponent<Collider>());
            go.name = "Staple";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<MeshRenderer>();
            if (metal != null) r.sharedMaterial = metal;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void Retire(Trap trap)
        {
            trap.Retired = true;
            Unblock(trap);
        }

        private void Unblock(Trap trap)
        {
            if (trap.Blocker == 0) return;
            PaintBlockers.Remove(trap.Blocker);
            trap.Blocker = 0;
        }

        private void Clear()
        {
            _pending = null;
            foreach (Trap t in _traps)
            {
                Unblock(t);
                if (t.Root != null) Destroy(t.Root);
            }
            _traps.Clear();
        }

        private void Update()
        {
            if (_pending != null && Time.timeScale > 0f) PlacePending();
            if (_traps.Count == 0) return;
            var state = GameServices.State;
            bool over = state != null && (state.IsGameWon || state.IsFinishing);
            float cycle = downSeconds + warnSeconds + upSeconds;
            Vector3 hero = transform.position;
            foreach (Trap t in _traps)
            {
                if (t.Root == null) continue;
                float rise;   // 0 = sunk, 1 = up
                Color slit = new(0.12f, 0.14f, 0.3f, 0.55f);
                Color red = new(0.92f, 0.17f, 0.12f, 1f);
                float fill;           // timer disc, 0-1 of the radius
                float fillAlpha = 0.22f, ringAlpha = 0.85f, ringWidth = 0.07f;
                if (t.Retired || over)
                {
                    rise = Mathf.MoveTowards(Rise(t), 0f, Time.deltaTime / moveSeconds);
                    slit.a = Mathf.MoveTowards(t.Slit.color.a, 0f, Time.deltaTime);
                    ringAlpha = Mathf.MoveTowards(t.Ring.startColor.a, 0f, Time.deltaTime * 2f);
                    fill = 0f;
                }
                else
                {
                    t.Clock = (t.Clock + Time.deltaTime) % cycle;
                    float c = t.Clock;
                    if (c < downSeconds)
                    {
                        rise = Mathf.Clamp01(1f - c / moveSeconds);   // sinking at the start
                        fill = c / downSeconds;                       // the timer fills up
                    }
                    else if (c < downSeconds + warnSeconds)
                    {
                        // Warning: full disc, the ring and slit flash and the tips peek out.
                        float k = (c - downSeconds) / warnSeconds;
                        float flash = 0.5f + 0.5f * Mathf.Sin(k * Mathf.PI * 6f);
                        slit = Color.Lerp(slit, new Color(0.95f, 0.2f, 0.15f, 0.8f), flash);
                        rise = 0.12f * flash;
                        fill = 1f;
                        fillAlpha = Mathf.Lerp(0.25f, 0.5f, flash);
                        ringWidth = Mathf.Lerp(0.08f, 0.15f, flash);
                    }
                    else
                    {
                        rise = Mathf.Clamp01((c - downSeconds - warnSeconds) / moveSeconds);
                        fill = 1f;
                        fillAlpha = 0.42f;
                        ringWidth = 0.12f;
                    }
                }
                t.Slit.color = slit;
                t.Timer.transform.localScale = Vector3.one * (radius * 2f * fill);
                t.Timer.color = new Color(red.r, red.g, red.b, fillAlpha * (t.Retired || over ? 0f : 1f));
                t.Ring.startColor = t.Ring.endColor = new Color(red.r, red.g, red.b, ringAlpha);
                t.Ring.widthMultiplier = ringWidth;
                t.Staples.localPosition = new Vector3(0f, Mathf.Lerp(-height - 0.1f, 0f, rise), 0f);
                // The paper does not hide what is under it, so sunk staples are switched off:
                // seen through the page they read as already up.
                bool show = rise > 0.01f;
                if (t.Staples.gameObject.activeSelf != show) t.Staples.gameObject.SetActive(show);

                bool up = rise > 0.5f && !t.Retired && !over;
                if (up && t.Blocker == 0) t.Blocker = PaintBlockers.Add(t.At, radius);
                else if (!up) Unblock(t);

                if (up && combat != null)
                {
                    Vector3 d = hero - t.At; d.y = 0f;
                    if (d.magnitude < radius && !combat.IsInvulnerable)
                    {
                        combat.TakeDamage(1);
                        if (knockback != null)
                            knockback.ApplyKnockback(d.sqrMagnitude > 1e-4f ? d : -transform.forward, knockbackForce);
                        Haptics.Medium();
                    }
                }
            }
        }

        private float Rise(Trap t) => Mathf.InverseLerp(-height - 0.1f, 0f, t.Staples.localPosition.y);

        // A crisp disc, 1 unit wide.
        private Sprite DiscSprite()
        {
            if (_discSprite != null) return _discSprite;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)))));
            tex.Apply();
            _discSprite = Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f, n);
            return _discSprite;
        }

        // A soft-edged slit: a dark stretched ellipse.
        private Sprite SlitSprite()
        {
            if (_slitSprite != null) return _slitSprite;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01((1f - d) * 4f)));
                }
            tex.Apply();
            _slitSprite = Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f, n);
            return _slitSprite;
        }
    }
}
