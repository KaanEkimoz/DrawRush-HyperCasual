using System.Collections;
using DrawRush.Core;
using DrawRush.Drawing;
using UnityEngine;

namespace DrawRush.Boosters
{
    /// <summary>
    /// The shop's drawing helper (the "NPC çizimci" a playtester asked for), kept small on purpose
    /// so the player still does the drawing (research: a helper that draws for you plays the game
    /// for you). Every so often it paints a short stretch of the unpainted gap nearest the player,
    /// for good, and it never touches the last unfinished edge.
    /// Owned and levelled in the shop; upgrades shorten the wait and lengthen the stroke.
    /// </summary>
    public sealed class BrushBuddy : MonoBehaviour
    {
        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private Material material;
        [SerializeField] private Color color = new(0.3f, 0.9f, 0.85f, 1f);
        [Tooltip("Seconds between strokes per level 1..3.")]
        [SerializeField] private float[] cooldowns = { 20f, 16f, 12f };
        [Tooltip("Stroke length (m) per level 1..3.")]
        [SerializeField] private float[] strokes = { 1.5f, 2f, 2.5f };
        [Tooltip("Seconds the stroke takes to draw.")]
        [SerializeField] private float strokeSeconds = 0.8f;
        [SerializeField] private Vector3 followOffset = new(0.9f, 0f, -0.7f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Transform _body;
        private float _ready;
        private bool _drawing;

        private void Awake() { if (painter == null) painter = GetComponent<FreeDrawPainter>(); }

        private void Start()
        {
            BoosterUpgrades.Changed += Refresh;
            if (painter != null) painter.LevelBound += OnLevelBound;
            Refresh();
        }

        private void OnDestroy()
        {
            BoosterUpgrades.Changed -= Refresh;
            if (painter != null) painter.LevelBound -= OnLevelBound;
        }

        private void OnLevelBound(EdgeNetwork _)
        {
            StopAllCoroutines();
            _drawing = false;
            _ready = Time.time + Cooldown();   // never an instant freebie at the start
        }

        private void Refresh()
        {
            bool owned = BoosterUpgrades.BuddyLevel > 0;
            if (owned && _body == null) Build();
            if (_body != null) _body.gameObject.SetActive(owned);
        }

        private int Level => Mathf.Clamp(BoosterUpgrades.BuddyLevel, 1, cooldowns.Length);
        private float Cooldown() => cooldowns[Level - 1];

        private void Build()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "BrushBuddy";
            Destroy(go.GetComponent<Collider>());
            go.transform.localScale = new Vector3(0.25f, 0.3f, 0.25f);
            go.transform.position = transform.position + followOffset;
            var r = go.GetComponent<Renderer>();
            if (material != null) r.sharedMaterial = material;
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor(BaseColorId, color);
            mpb.SetColor(ColorId, color);
            r.SetPropertyBlock(mpb);
            _body = go.transform;
        }

        private void Update()
        {
            if (_body == null || !_body.gameObject.activeSelf || _drawing) return;
            Vector3 home = transform.position + followOffset;
            Vector3 target = new Vector3(home.x, 0.5f + 0.06f * Mathf.Sin(Time.time * 4f), home.z);
            _body.position = Vector3.Lerp(_body.position, target, 1f - Mathf.Exp(-8f * Time.deltaTime));

            var state = GameServices.State;
            if (Time.time < _ready || painter == null || painter.Network == null || (state != null && state.IsGameWon)) return;
            if (TryPickStroke(out DrawEdge edge, out float from, out float to)) StartCoroutine(Stroke(edge, from, to));
            else _ready = Time.time + 2f;
        }

        // The unpainted gap nearest the player, entered at its nearer end; never on the last edge.
        private bool TryPickStroke(out DrawEdge best, out float from, out float to)
        {
            best = null; from = to = 0f;
            int open = 0;
            foreach (DrawEdge e in painter.Network.Edges) if (!e.IsComplete) open++;
            if (open <= 1) return false;

            float bestSqr = float.PositiveInfinity;
            Vector3 p = transform.position;
            foreach (DrawEdge e in painter.Network.Edges)
            {
                if (e.IsComplete) continue;
                float len = e.Length;
                if (len < 0.01f) continue;
                float cursor = 0f;
                var spans = e.Fill.Spans;
                for (int i = 0; i <= spans.Count; i++)
                {
                    float g0 = cursor, g1 = i < spans.Count ? spans[i].x : 1f;
                    if (i < spans.Count) cursor = Mathf.Max(cursor, spans[i].y);
                    if (g1 - g0 < 0.001f) continue;
                    float d0 = (e.PointAt(g0) - p).sqrMagnitude, d1 = (e.PointAt(g1) - p).sqrMagnitude;
                    if (Mathf.Min(d0, d1) >= bestSqr) continue;
                    bestSqr = Mathf.Min(d0, d1);
                    best = e;
                    float reach = Mathf.Min(strokes[Level - 1] / len, g1 - g0);
                    if (d0 <= d1) { from = g0; to = g0 + reach; } else { from = g1; to = g1 - reach; }
                }
            }
            return best != null;
        }

        // Fly along the stroke, painting behind it.
        private IEnumerator Stroke(DrawEdge edge, float from, float to)
        {
            _drawing = true;
            float t = 0f, painted = from;
            while (t < 1f && edge != null && !edge.IsComplete)
            {
                t = Mathf.Min(1f, t + Time.deltaTime / Mathf.Max(0.05f, strokeSeconds));
                float at = Mathf.Lerp(from, to, t);
                Vector3 pos = edge.PointAt(at);
                _body.position = new Vector3(pos.x, 0.4f, pos.z);
                edge.PaintKept(painted, at);
                painted = at;
                yield return null;
            }
            _drawing = false;
            _ready = Time.time + Cooldown();
        }
    }
}
