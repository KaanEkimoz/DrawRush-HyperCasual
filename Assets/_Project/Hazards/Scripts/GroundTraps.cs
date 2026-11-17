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
    /// Traps on the shape's line, so a long straight edge is no longer one easy run. Both block
    /// the pencil (no paint where they are) and hurt on touch.
    /// <list type="bullet">
    /// <item>Spikes: a cluster of steel spikes that rise out of holes in the paper, stay up a
    /// while and sink again. The holes always show where they are; just before they rise the tips
    /// peek out and shake.</item>
    /// <item>Saw: a spinning blade lying just above the paper (the camera looks down, so a
    /// standing blade would read as a line), sliding back and forth along a slot across the
    /// line. Pass behind it.</item>
    /// <item>Roller: a giant highlighter lying across the line, rolling back and forth along it
    /// between two stoppers. Follow it and draw behind it.</item>
    /// </list>
    /// When the edge under a trap is finished the trap sinks for good (the wall rises there).
    /// Placed fresh each level from a set level on, more of them later in the campaign.
    /// </summary>
    public sealed class GroundTraps : MonoBehaviour
    {
        public enum Kind { Spikes, Saw, Roller }

        /// <summary>A spike trap came up / went down (its centre). For sound, which lives elsewhere.</summary>
        public static event System.Action<Vector3> SpikesRose, SpikesSank;
        /// <summary>0-1: how close the nearest working saw is to the hero (0 = no saw).</summary>
        public static float SawLevel { get; private set; }
        /// <summary>0-1: the nearest working roller's loudness (closeness × how fast it is rolling).</summary>
        public static float RollerLevel { get; private set; }
        /// <summary>0-1: how fast that roller is rolling (it slows to a stop at each end).</summary>
        public static float RollerSpeed { get; private set; }
        private static Vector3 _hero;

        /// <summary>Loudness share for a trap sound at <paramref name="at"/>: full beside the
        /// hero, a quarter across the page.</summary>
        public static float Nearness(Vector3 at)
        {
            at.y = 0f; Vector3 h = _hero; h.y = 0f;
            return Mathf.Lerp(1f, 0.25f, Mathf.InverseLerp(1.5f, 9f, Vector3.Distance(at, h)));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { SpikesRose = null; SpikesSank = null; SawLevel = 0f; RollerLevel = 0f; RollerSpeed = 0f; }

        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerKnockback knockback;
        [SerializeField] private BoosterHud hud;
        [Tooltip("Trap steel. A direct reference keeps the shader in the build.")]
        [SerializeField] private Material metal;
        [Tooltip("Highlighter body and cap.")]
        [SerializeField] private Material rollerBody;
        [SerializeField] private Material rollerCap;

        [Header("Placement (which traps a level gets: see Campaign)")]
        [Tooltip("Edges shorter than this (m) never get a trap: no room to go around it.")]
        [SerializeField] private float minEdgeMeters = 3f;
        [Tooltip("No trap starts closer than this (m) to the hero.")]
        [SerializeField] private float clearOfHero = 2.5f;

        [Header("Spikes (seconds)")]
        // New field names on purpose (Kaan, 2026-11-17: 2.5 s under, 2.5 s up), so older values
        // saved in the scene don't override them.
        [Tooltip("Seconds the spikes stay under the paper each cycle (includes sinking).")]
        [SerializeField] private float hiddenSeconds = 2.5f;
        [Tooltip("Seconds the spikes stay out each cycle (includes rising).")]
        [SerializeField] private float activeSeconds = 2.5f;
        [SerializeField] private float moveSeconds = 0.28f;
        [Tooltip("Ground radius (m) that blocks paint and hurts while up.")]
        [SerializeField] private float radius = 0.6f;
        [SerializeField] private float height = 0.6f;

        [Header("Saw")]
        [Tooltip("Slot length (m), across the line.")]
        [SerializeField] private float sawTrack = 3.4f;
        [SerializeField] private float sawRadius = 0.6f;
        [Tooltip("Seconds for one trip there and back.")]
        [SerializeField] private float sawPeriod = 3.4f;
        [SerializeField] private float sawSpin = 720f;

        [Header("Roller")]
        [Tooltip("Length (m) of the stretch of line it rolls along.")]
        [SerializeField] private float rollerSpan = 3.2f;
        [SerializeField] private float rollerRadius = 0.28f;
        [SerializeField] private float rollerLength = 1.7f;
        [Tooltip("Seconds for one trip there and back.")]
        [SerializeField] private float rollerPeriod = 3.2f;   // rolling time for a there-and-back
        [Tooltip("Seconds it rests at each end before rolling back (and goes quiet).")]
        [SerializeField] private float rollerHold = 0.6f;

        [SerializeField] private float knockbackForce = 7f;

        private readonly List<Trap> _traps = new();
        private int _testHops;          // trap test: shapes skipped looking for a straight edge
        private EdgeNetwork _pending;   // bound, not placed yet: waits for the countdown to end
        private Sprite _dot, _square;
        private static Mesh _spikeMesh, _bladeMesh;

        private sealed class Trap
        {
            public Kind Kind;
            public DrawEdge Edge;
            public Vector3 At;
            public Vector3 Across;     // saw track direction (ground)
            public Vector3 Along;      // roller track direction (ground)
            public float Half;         // roller: half its stretch (m)
            public GameObject Root;
            public Transform Moving;   // spikes group / saw blade
            public Transform Shadow;   // saw: the dark disc under the blade
            public float Clock;
            public int Blocker;        // 0 = not blocking
            public bool Retired;
            public float Sink;         // 0-1 of the way down, once retired
            public int Phase = -1;     // spikes: 0 down, 1 coming, 2 up (sounds fire on a change)
        }

        /// <summary>The trap kind for a campaign kind (the schedule lives in <see cref="Campaign"/>).</summary>
        public static Kind KindOf(Novelty n) => n == Novelty.Saw ? Kind.Saw : n == Novelty.Roller ? Kind.Roller : Kind.Spikes;

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
            if (TrapTest.Active)
            {
                // The roller only rolls straight: on an all-curves shape, move on to the next
                // shape that has a straight edge (each switch binds again and lands back here).
                if (!HasStraightEdge(network) && _testHops < 64)
                {
                    var levels = FindFirstObjectByType<LevelManager>();
                    if (levels != null && levels.LevelCount > 1)
                    {
                        _testHops++;
                        int next = levels.CurrentIndex;
                        do next = (next + 1) % levels.LevelCount; while (next == levels.TutorialIndex);
                        levels.ActivateLevel(next);
                        return;
                    }
                }
                _testHops = 0;
                // Every kind on this shape and nothing else on the page.
                Transform levelRoot = network.transform.parent != null ? network.transform.parent : network.transform;
                LevelDifficulty.ApplyEnemyBudget(levelRoot, 0);
                Place(network, 4, level);
                if (hud != null) hud.Toast("TRAP TEST", "Spikes, saw, roller", new Color(0.45f, 0.5f, 0.6f));
                return;
            }
            Transform shapeRoot = network.transform.parent != null ? network.transform.parent : network.transform;
            int count = Campaign.Traps(level, LevelDifficulty.ShapeScore(shapeRoot));
            if (count <= 0) return;
            Place(network, count, level);
            if (hud == null || _traps.Count == 0) return;
            var c = new Color(0.45f, 0.5f, 0.6f);
            switch (Campaign.DebutAt(level))
            {
                case Novelty.Spikes: hud.Toast("NEW: SPIKE TRAP", "Spikes rise and sink: time your run", c); break;
                case Novelty.Saw: hud.Toast("NEW: SAW", "It slides back and forth: pass behind it", c); break;
                case Novelty.Roller: hud.Toast("NEW: ROLLER", "It rolls along the line: draw behind it", c); break;
            }
        }

        private void Place(EdgeNetwork network, int count, int level)
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
            Transform levelRoot = network.transform.parent != null ? network.transform.parent : network.transform;
            foreach (Transform c in levelRoot.GetComponentsInChildren<Transform>())
                if (c.name == "EdgeDrop") drops.Add(c.position);
            foreach (DrawEdge e in edges)
            {
                if (_traps.Count >= count) break;
                Kind kind = TrapTest.Active ? TestKinds[_traps.Count % TestKinds.Length]
                    : KindOf(Campaign.TrapKind(level, _traps.Count));
                if (kind == Kind.Roller && e.IsArc) kind = Kind.Spikes;   // it rolls straight
                float keepOff = kind == Kind.Saw ? sawRadius : kind == Kind.Roller ? 0.3f : radius;
                // The test level crowds every kind onto one shape, so it packs them closer.
                float apart = TrapTest.Active ? 2.2f : 3.5f;
                for (int attempt = 0; attempt < (TrapTest.Active ? 30 : 12); attempt++)
                {
                    Vector3 at = e.PointAt(Random.Range(0.25f, 0.75f));
                    at.y = 0f;
                    if (Flat(at - hero) < clearOfHero) continue;
                    bool onDrop = false;
                    foreach (Vector3 d in drops) if (Flat(at - d) < keepOff + 0.45f) { onDrop = true; break; }
                    if (onDrop) continue;
                    bool crowded = false;
                    foreach (Trap t in _traps) if (Flat(at - t.At) < apart) { crowded = true; break; }
                    if (crowded) continue;
                    _traps.Add(kind == Kind.Saw ? BuildSaw(e, at, network.transform)
                        : kind == Kind.Roller ? BuildRoller(e, at, network.transform)
                        : BuildSpikes(e, at, network.transform));
                    break;
                }
            }
        }

        private bool HasStraightEdge(EdgeNetwork network)
        {
            foreach (DrawEdge e in network.Edges) if (!e.IsArc && e.Length >= minEdgeMeters) return true;
            return false;
        }

        private static readonly Kind[] TestKinds = { Kind.Roller, Kind.Saw, Kind.Spikes, Kind.Spikes };

        private static float Flat(Vector3 v) { v.y = 0f; return v.magnitude; }

        private static Quaternion Facing(DrawEdge edge, Vector3 at, out Vector3 along)
        {
            float t = edge.ClosestT(at, out _);
            along = edge.TangentAt(t); along.y = 0f;
            if (along.sqrMagnitude < 1e-6f) along = Vector3.forward;
            along.Normalize();
            return Quaternion.LookRotation(along, Vector3.up);
        }

        private Trap NewTrap(Kind kind, DrawEdge edge, Vector3 at, Transform parent, out GameObject root, out Vector3 along)
        {
            root = new GameObject(kind + "Trap");
            root.transform.SetParent(parent, true);
            root.transform.SetPositionAndRotation(at, Facing(edge, at, out along));
            var trap = new Trap { Kind = kind, Edge = edge, At = at, Root = root };
            edge.Completed += _ => Retire(trap);
            return trap;
        }

        private Trap BuildSpikes(DrawEdge edge, Vector3 at, Transform parent)
        {
            Trap trap = NewTrap(Kind.Spikes, edge, at, parent, out GameObject root, out _);
            var spikes = new GameObject("Spikes").transform;
            spikes.SetParent(root.transform, false);
            // A centre spike and two rings around it, each with the hole it comes out of.
            var spots = new List<Vector2> { Vector2.zero };
            for (int i = 0; i < 6; i++) spots.Add(Ring(i, 6, radius * 0.45f, 0f));
            for (int i = 0; i < 8; i++) spots.Add(Ring(i, 8, radius * 0.82f, 0.4f));
            foreach (Vector2 s in spots)
            {
                float h = height * (s == Vector2.zero ? 1f : s.magnitude < radius * 0.6f ? 0.9f : 0.75f);
                var spike = new GameObject("Spike").transform;
                spike.SetParent(spikes, false);
                spike.localPosition = new Vector3(s.x, 0f, s.y);
                spike.localScale = new Vector3(0.17f, h, 0.17f);
                spike.gameObject.AddComponent<MeshFilter>().sharedMesh = SpikeMesh();
                var r = spike.gameObject.AddComponent<MeshRenderer>();
                if (metal != null) r.sharedMaterial = metal;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var hole = new GameObject("Hole").AddComponent<SpriteRenderer>();
                hole.sprite = Dot();
                hole.color = HoleColor;
                hole.transform.SetParent(root.transform, false);
                hole.transform.localPosition = new Vector3(s.x, 0.035f, s.y);
                hole.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                hole.transform.localScale = Vector3.one * HoleSize;
            }
            // They grow out of the holes (height scales from the ground up), never sit below it:
            // the paper does not hide what is under it.
            spikes.localScale = new Vector3(1f, 0.001f, 1f);
            spikes.gameObject.SetActive(false);
            trap.Moving = spikes;
            trap.Clock = Random.Range(0f, hiddenSeconds);   // traps do not all rise together
            return trap;
        }

        private static Vector2 Ring(int i, int n, float r, float offset)
        {
            float a = (i + offset) / n * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
        }

        private Trap BuildSaw(DrawEdge edge, Vector3 at, Transform parent)
        {
            Trap trap = NewTrap(Kind.Saw, edge, at, parent, out GameObject root, out Vector3 along);
            trap.Across = Vector3.Cross(Vector3.up, along);

            // The slot in the paper it runs in, across the line.
            var slot = new GameObject("Slot").AddComponent<SpriteRenderer>();
            slot.sprite = Square();
            slot.color = new Color(0.16f, 0.17f, 0.22f, 0.85f);
            slot.transform.SetParent(root.transform, false);
            slot.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            slot.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            slot.transform.localScale = new Vector3(sawTrack, 0.09f, 1f);

            // The blade lies flat over the slot and spins about the vertical.
            var blade = new GameObject("Blade").transform;
            blade.SetParent(root.transform, false);
            blade.gameObject.AddComponent<MeshFilter>().sharedMesh = BladeMesh();
            var r = blade.gameObject.AddComponent<MeshRenderer>();
            if (metal != null) r.sharedMaterial = metal;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            blade.localScale = Vector3.one * sawRadius;
            trap.Moving = blade;
            // A shadow under it: steel on pale paper needs the contrast.
            var shadow = new GameObject("SawShadow").AddComponent<SpriteRenderer>();
            shadow.sprite = Dot();
            shadow.color = new Color(0.12f, 0.13f, 0.2f, 0.4f);
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = Vector3.one * (sawRadius * 2.15f);
            trap.Shadow = shadow.transform;
            trap.Clock = Random.Range(0f, sawPeriod);
            trap.Blocker = PaintBlockers.Add(at, sawRadius);
            return trap;
        }

        private Trap BuildRoller(DrawEdge edge, Vector3 at, Transform parent)
        {
            Trap trap = NewTrap(Kind.Roller, edge, at, parent, out GameObject root, out Vector3 along);
            trap.Along = along;
            trap.Half = Mathf.Min(rollerSpan, edge.Length * 0.55f) * 0.5f;

            // Two stoppers across the line mark the stretch it rolls along.
            for (int end = -1; end <= 1; end += 2)
            {
                var stop = new GameObject("Stopper").AddComponent<SpriteRenderer>();
                stop.sprite = Square();
                stop.color = new Color(0.16f, 0.17f, 0.22f, 0.85f);
                stop.transform.SetParent(root.transform, false);
                stop.transform.localPosition = new Vector3(0f, 0.035f, end * (trap.Half + rollerRadius + 0.08f));
                stop.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                stop.transform.localScale = new Vector3(rollerLength * 0.8f, 0.1f, 1f);
            }

            // The highlighter: body, a cap at one end and a clip that shows it turning.
            var body = new GameObject("Roller").transform;
            body.SetParent(root.transform, false);
            Part(body, PrimitiveType.Cylinder, Vector3.zero, Quaternion.Euler(0f, 0f, 90f),
                new Vector3(rollerRadius * 2f, rollerLength * 0.5f, rollerRadius * 2f), rollerBody);
            Part(body, PrimitiveType.Cylinder, new Vector3(rollerLength * 0.42f, 0f, 0f), Quaternion.Euler(0f, 0f, 90f),
                new Vector3(rollerRadius * 2.12f, rollerLength * 0.1f, rollerRadius * 2.12f), rollerCap);
            Part(body, PrimitiveType.Cube, new Vector3(rollerLength * 0.25f, rollerRadius * 1.02f, 0f), Quaternion.identity,
                new Vector3(rollerLength * 0.32f, 0.06f, 0.1f), rollerCap);
            trap.Moving = body;
            trap.Clock = Random.Range(0f, rollerPeriod + 2f * rollerHold);
            trap.Blocker = PaintBlockers.Add(at, rollerRadius + 0.3f);
            return trap;
        }

        private static void Part(Transform parent, PrimitiveType type, Vector3 pos, Quaternion rot, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.SetLocalPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            if (mat != null) r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // Where the roller is (-1..1 along its stretch) and how fast it is going (0-1): it eases
        // from one end to the other, then rests there for rollerHold before rolling back.
        private float RollerAt(float clock, out float speed)
        {
            float move = rollerPeriod * 0.5f;
            float leg = move + rollerHold;
            float c = Mathf.Repeat(clock, leg * 2f);
            float dir = c < leg ? 1f : -1f;
            float k = Mathf.Clamp01((c < leg ? c : c - leg) / move);
            speed = Mathf.Sin(k * Mathf.PI);
            return -dir + 2f * dir * (0.5f - 0.5f * Mathf.Cos(k * Mathf.PI));
        }

        private void UpdateRoller(Trap t, Vector3 hero)
        {
            if (!t.Retired) t.Clock += Time.deltaTime;
            float s = RollerAt(t.Clock, out float speed) * t.Half;
            if (t.Retired) t.Sink = Mathf.MoveTowards(t.Sink, 1f, Time.deltaTime * 2f);
            // Retired, it shrinks away (a highlighter cannot sink into the page).
            t.Moving.localScale = Vector3.one * (1f - t.Sink);
            t.Moving.localPosition = new Vector3(0f, rollerRadius * (1f - t.Sink), s);
            // Rolls without slipping: turned by the distance travelled over its radius.
            t.Moving.localRotation = Quaternion.AngleAxis(s / rollerRadius * Mathf.Rad2Deg, Vector3.right);
            if (t.Sink >= 1f && t.Moving.gameObject.activeSelf) t.Moving.gameObject.SetActive(false);
            if (t.Retired) return;

            Vector3 ground = t.At + t.Along * s;
            if (t.Blocker != 0) PaintBlockers.Move(t.Blocker, ground);
            float level = Nearness(ground) * speed;   // silent while it rests at an end
            if (level > RollerLevel) { RollerLevel = level; RollerSpeed = speed; }
            if (combat == null || combat.IsInvulnerable) return;
            Vector3 d = hero - ground; d.y = 0f;
            float a = Vector3.Dot(d, t.Along);
            float across = Vector3.Dot(d, Vector3.Cross(Vector3.up, t.Along));
            if (Mathf.Abs(a) >= rollerRadius + 0.25f || Mathf.Abs(across) >= rollerLength * 0.5f + 0.15f) return;
            combat.TakeDamage(1);
            if (knockback != null) knockback.ApplyKnockback(Mathf.Abs(a) > 0.01f ? t.Along * Mathf.Sign(a) : t.Along, knockbackForce);
            Haptics.Medium();
        }

        private void Retire(Trap trap)
        {
            trap.Retired = true;
            Unblock(trap);
        }

        // Trap test panel: one big switch per trap kind, to look at and listen to each alone.
        private readonly bool[] _testOn = { true, true, true };
        private GUIStyle _testButton;

        private void OnGUI()
        {
            if (!TrapTest.Active) return;
            float u = Screen.height / 1280f;   // the game view is portrait; size the panel to it
            if (_testButton == null) _testButton = new GUIStyle(GUI.skin.button);
            _testButton.fontSize = Mathf.RoundToInt(30 * u);
            _testButton.fontStyle = FontStyle.Bold;
            string[] names = { "Spikes", "Saw", "Roller" };
            float w = 230 * u, h = 70 * u, x = Screen.width - w - 16 * u, y = 170 * u;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(x, y + i * (h + 10 * u), w, h);
                GUI.color = _testOn[i] ? new Color(0.2f, 0.62f, 0.3f) : new Color(0.75f, 0.2f, 0.2f);
                GUI.DrawTexture(r, Texture2D.whiteTexture);   // solid, so ON / OFF reads at a glance
                GUI.color = Color.white;
                if (GUI.Button(r, names[i] + (_testOn[i] ? ": ON" : ": OFF"), _testButton)) _testOn[i] = !_testOn[i];
            }
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
            SawLevel = 0f;
            RollerLevel = 0f;
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
            _hero = transform.position;
            SawLevel = 0f;
            RollerLevel = 0f;
            if (_traps.Count == 0) return;
            var state = GameServices.State;
            bool over = state != null && (state.IsGameWon || state.IsFinishing);
            Vector3 hero = transform.position;
            foreach (Trap t in _traps)
            {
                if (t.Root == null) continue;
                if (over && !t.Retired) Retire(t);
                // Trap test: a kind switched off on the panel vanishes (and stops blocking, hurting, sounding).
                bool on = !TrapTest.Active || _testOn[(int)t.Kind];
                if (t.Root.activeSelf != on) t.Root.SetActive(on);
                if (!on) { Unblock(t); t.Phase = -1; continue; }
                if (t.Kind == Kind.Saw) UpdateSaw(t, hero);
                else if (t.Kind == Kind.Roller) UpdateRoller(t, hero);
                else UpdateSpikes(t, hero);
            }
        }

        private void UpdateSpikes(Trap t, Vector3 hero)
        {
            // The last 0.7 s under the paper is the "coming" phase (no visible tell; sound logic).
            float warnSeconds = Mathf.Min(0.7f, hiddenSeconds * 0.5f);
            float downSeconds = hiddenSeconds - warnSeconds;
            float cycle = hiddenSeconds + activeSeconds;
            float rise;   // 0 = sunk, 1 = up
            int phase;
            if (t.Retired)
            {
                rise = Mathf.MoveTowards(Rise(t), 0f, Time.deltaTime / moveSeconds);
                phase = 0;
            }
            else
            {
                t.Clock = (t.Clock + Time.deltaTime) % cycle;
                float c = t.Clock;
                phase = c < downSeconds ? 0 : c < downSeconds + warnSeconds ? 1 : 2;
                if (c < downSeconds)
                {
                    float k = Mathf.Clamp01(c / moveSeconds);
                    rise = 1f - k * k;   // sinking back in at the start, slow then quick
                }
                else if (c < downSeconds + warnSeconds) rise = 0f;   // still down, no tell
                else
                {
                    // Out with a small overshoot, so the pop reads.
                    float k = Mathf.Clamp01((c - downSeconds - warnSeconds) / moveSeconds);
                    rise = 1f + Mathf.Sin(k * Mathf.PI) * 0.15f * k - (1f - k) * (1f - k);
                    rise = Mathf.Max(0f, rise);
                }
            }
            t.Moving.localScale = new Vector3(1f, Mathf.Max(0.001f, rise), 1f);
            if (phase != t.Phase)
            {
                if (phase == 2) SpikesRose?.Invoke(t.At);
                else if (phase == 0 && t.Phase == 2) SpikesSank?.Invoke(t.At);
                t.Phase = phase;
            }
            bool show = rise > 0.01f;
            if (t.Moving.gameObject.activeSelf != show) t.Moving.gameObject.SetActive(show);

            bool up = rise > 0.5f && !t.Retired;
            if (up && t.Blocker == 0) t.Blocker = PaintBlockers.Add(t.At, radius);
            else if (!up) Unblock(t);
            if (up) Hurt(hero, t.At, radius);
        }

        private static float Rise(Trap t) => Mathf.Clamp01(t.Moving.localScale.y);

        private const float HoleSize = 0.19f;
        private static readonly Color HoleColor = new Color(0.16f, 0.17f, 0.22f, 0.8f);

        private void UpdateSaw(Trap t, Vector3 hero)
        {
            float half = sawTrack * 0.5f - sawRadius * 0.6f;
            if (!t.Retired) t.Clock += Time.deltaTime;
            float s = Mathf.Sin(t.Clock / sawPeriod * Mathf.PI * 2f) * half;
            if (t.Retired) t.Sink = Mathf.MoveTowards(t.Sink, 1f, Time.deltaTime * 2f);
            float y = Mathf.Lerp(0.12f, -0.1f, t.Sink);
            t.Moving.localPosition = new Vector3(s, y, 0f);
            t.Shadow.localPosition = new Vector3(s, 0.04f, 0f);
            if (t.Sink >= 1f && t.Shadow.gameObject.activeSelf) t.Shadow.gameObject.SetActive(false);
            if (!t.Retired) t.Moving.localRotation = Quaternion.Euler(0f, t.Clock * sawSpin, 0f) * Quaternion.Euler(90f, 0f, 0f);
            if (t.Sink >= 1f && t.Moving.gameObject.activeSelf) t.Moving.gameObject.SetActive(false);
            if (t.Retired) return;

            Vector3 ground = t.At + t.Across * s;
            if (t.Blocker != 0) PaintBlockers.Move(t.Blocker, ground);
            SawLevel = Mathf.Max(SawLevel, Nearness(ground));
            Hurt(hero, ground, sawRadius);
        }

        private void Hurt(Vector3 hero, Vector3 at, float r)
        {
            if (combat == null) return;
            Vector3 d = hero - at; d.y = 0f;
            if (d.magnitude >= r || combat.IsInvulnerable) return;
            combat.TakeDamage(1);
            if (knockback != null) knockback.ApplyKnockback(d.sqrMagnitude > 1e-4f ? d : -transform.forward, knockbackForce);
            Haptics.Medium();
        }

        // A unit cone: base radius 0.5 at y = 0, tip at y = 1.
        private static Mesh SpikeMesh()
        {
            if (_spikeMesh != null) return _spikeMesh;
            const int n = 10;
            var v = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i < n; i++)
            {
                float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
                int b = v.Count;
                v.Add(new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f));
                v.Add(new Vector3(0f, 1f, 0f));
                v.Add(new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f));
                tri.Add(b); tri.Add(b + 1); tri.Add(b + 2);
            }
            _spikeMesh = new Mesh { name = "Auto_Spike" };
            _spikeMesh.SetVertices(v);
            _spikeMesh.SetTriangles(tri, 0);
            _spikeMesh.RecalculateNormals();
            _spikeMesh.RecalculateBounds();
            return _spikeMesh;
        }

        // A flat saw blade of radius 1 in the XY plane, with hooked teeth and a hole, both faces.
        private static Mesh BladeMesh()
        {
            if (_bladeMesh != null) return _bladeMesh;
            const int teeth = 14;
            var rim = new List<Vector3>();
            for (int i = 0; i < teeth; i++)
            {
                float a = i / (float)teeth * Mathf.PI * 2f, step = Mathf.PI * 2f / teeth;
                rim.Add(new Vector3(Mathf.Cos(a) * 0.8f, Mathf.Sin(a) * 0.8f, 0f));                      // gullet
                rim.Add(new Vector3(Mathf.Cos(a + step * 0.75f) * 1f, Mathf.Sin(a + step * 0.75f) * 1f, 0f)); // tip
            }
            const float hole = 0.18f;
            var v = new List<Vector3>();
            var tri = new List<int>();
            for (int face = 0; face < 2; face++)
            {
                float z = face == 0 ? -0.025f : 0.025f;
                int b = v.Count;
                for (int i = 0; i < rim.Count; i++)
                {
                    Vector3 o = rim[i];
                    v.Add(new Vector3(o.x, o.y, z));
                    Vector3 inner = o.normalized * hole;
                    v.Add(new Vector3(inner.x, inner.y, z));
                }
                for (int i = 0; i < rim.Count; i++)
                {
                    int o0 = b + i * 2, i0 = o0 + 1, o1 = b + ((i + 1) % rim.Count) * 2, i1 = o1 + 1;
                    if (face == 0) { tri.Add(o0); tri.Add(i0); tri.Add(o1); tri.Add(o1); tri.Add(i0); tri.Add(i1); }
                    else { tri.Add(o0); tri.Add(o1); tri.Add(i0); tri.Add(o1); tri.Add(i1); tri.Add(i0); }
                }
            }
            _bladeMesh = new Mesh { name = "Auto_SawBlade" };
            _bladeMesh.SetVertices(v);
            _bladeMesh.SetTriangles(tri, 0);
            _bladeMesh.RecalculateNormals();
            _bladeMesh.RecalculateBounds();
            return _bladeMesh;
        }

        private Sprite Dot()
        {
            if (_dot != null) return _dot;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)))));
            tex.Apply();
            _dot = Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f, n);
            return _dot;
        }

        private Sprite Square()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);
            return _square;
        }
    }
}
