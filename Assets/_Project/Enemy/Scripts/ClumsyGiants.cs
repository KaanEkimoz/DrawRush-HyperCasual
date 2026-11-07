using System;
using System.Collections;
using System.Collections.Generic;
using DrawRush.Boosters;
using DrawRush.Core;
using DrawRush.Drawing;
using DrawRush.Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DrawRush.Enemy
{
    /// <summary>
    /// The notebook's owners, who never notice the game on their desk. Now and then they cause a
    /// hazard: a coffee cup lands (its shadow warns first; the hit hurts and the spill slows you),
    /// the table gets bumped (every enemy skids the same way, the desk jolts), a fountain pen drips
    /// ink on the shape's line (the pencil cannot draw on wet ink for a while), a desk fan gusts
    /// (everything drifts downwind) or a ruler is swept across the page (step out of its red lane).
    /// The later ones join the roster level by level. Never during
    /// the opening seconds of a level, and only from a set level on. Nothing outlives the level:
    /// a win or the hero's death calls off whatever is still in the air, and a revive starts over.
    /// </summary>
    public sealed class ClumsyGiants : MonoBehaviour
    {
        /// <summary>The table was bumped; carries the ground direction everything slides.</summary>
        public static event Action<Vector3> TableBumped;

        /// <summary>Shake the desk without a bump (camera shake and thud only): the Giant Stapler's landing.</summary>
        public static void Thud(Vector3 direction) => TableBumped?.Invoke(direction);

        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private ThirdPersonMovement movement;
        [SerializeField] private BoosterHud hud;
        [SerializeField] private PlayerKnockback knockback;

        [Header("Timing (which levels get accidents, and which kinds: see Campaign)")]
        [SerializeField] private float firstDelay = 7f;
        [SerializeField] private Vector2 interval = new(6.5f, 10f);

        [Header("Coffee cup")]
        [SerializeField] private Sprite cupSprite;
        [SerializeField] private Sprite stainSprite;
        [Tooltip("Seconds the shadow grows before the cup lands.")]
        [SerializeField] private float warnSeconds = 1.3f;
        [Tooltip("Radius (m) of the landing and of the spill.")]
        [SerializeField] private float radius = 1.6f;
        [SerializeField] private float stainSeconds = 6f;
        [Tooltip("Move speed multiple on the spill.")]
        [SerializeField] private float stainSlow = 0.6f;
        [Tooltip("How far ahead of the player (m) the cup aims.")]
        [SerializeField] private float leadMeters = 1.5f;

        [Header("Table bump")]
        [SerializeField] private float bumpPush = 2.5f;
        [Tooltip("Seconds the skid lasts.")]
        [SerializeField] private float slideSeconds = 0.7f;
        [Tooltip("Hero slide distance as a multiple of the enemies' (bumpPush).")]
        [SerializeField] private float heroSlideMultiplier = 0.7f;
        [Tooltip("How far (m) the desk decor jolts along the bump.")]
        [SerializeField] private float decorJolt = 0.45f;
        [SerializeField] private Color skidColor = new(0.12f, 0.16f, 0.42f, 0.45f);

        [Header("Ink drip")]
        [SerializeField] private float inkWarnSeconds = 1.1f;
        [Tooltip("Radius (m) of the wet blot the pencil cannot draw on.")]
        [SerializeField] private float inkRadius = 0.9f;
        [SerializeField] private float inkSeconds = 5f;
        [Tooltip("How far ahead of the hero (m) the drip aims, on the nearest unfinished edge.")]
        [SerializeField] private float inkLead = 2.5f;
        [SerializeField] private Color inkColor = new(0.1f, 0.13f, 0.38f, 0.92f);

        [Header("Fan gust")]
        [SerializeField] private float gustSeconds = 2.4f;
        [Tooltip("Peak drift speed (m/s) the gust adds to the hero.")]
        [SerializeField] private float gustSpeed = 2.6f;

        [Header("Ruler sweep")]
        [Tooltip("The desk ruler; swept across standing on its edge.")]
        [SerializeField] private GameObject rulerPrefab;
        [SerializeField] private float rulerWarnSeconds = 1.4f;
        [SerializeField] private float rulerSpeed = 10f;
        [SerializeField] private float rulerKnockback = 9f;
        [Tooltip("Ruler size as a multiple of the desk prop (7 m): its lane must leave room to dodge.")]
        [SerializeField] private float rulerScale = 0.7f;

        private Coroutine _loop;
        private Vector3 _lastPos, _heading;
        private GameObject _stain;
        private Vector3 _stainAt;
        private float _stainUntil;
        private GameObject _shadow, _cup;
        private readonly List<GameObject> _fx = new();
        private readonly List<int> _blockers = new();
        private Hazard _last = Hazard.None;
        private static Material _windMaterial;
        private Transform _decor;
        private Vector3 _decorHome;
        private readonly List<TrailRenderer> _skids = new();
        private static Material _skidMaterial;
        private GameState _state;
        private PlayerHealth _health;
        private bool _called;   // hazards were running when the level ended

        private void Awake()
        {
            if (knockback == null) knockback = GetComponent<PlayerKnockback>();
            if (painter == null) painter = GetComponent<FreeDrawPainter>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (movement == null) movement = GetComponent<ThirdPersonMovement>();
        }

        private void OnEnable() { if (painter != null) painter.LevelBound += OnLevelBound; }

        private void OnDisable()
        {
            if (painter != null) painter.LevelBound -= OnLevelBound;
            Unwatch();
            CallOff();
        }

        private void OnLevelBound(EdgeNetwork _)
        {
            Watch();
            CallOff();
            _called = false;
            _loop = Campaign.EventsOn(PlayerProgress.LevelsPlayed) && !DrawRush.Hazards.TrapTest.Active ? StartCoroutine(Loop()) : null;
        }

        // The services are registered after this component wakes, so they are picked up per level.
        private void Watch()
        {
            Unwatch();
            _state = GameServices.State;
            _health = GameServices.Health;
            if (_state != null) _state.GameWonChanged += OnWonChanged;
            if (_health != null) { _health.Died += OnLevelOver; _health.Changed += OnHealthChanged; }
        }

        private void Unwatch()
        {
            if (_state != null) _state.GameWonChanged -= OnWonChanged;
            if (_health != null) { _health.Died -= OnLevelOver; _health.Changed -= OnHealthChanged; }
            _state = null;
            _health = null;
        }

        private void OnWonChanged(bool won) { if (won) OnLevelOver(); }

        private void OnLevelOver()
        {
            _called = _loop != null;
            CallOff();
        }

        // A revive refills the hero in the same level, without binding it again.
        private void OnHealthChanged(int value)
        {
            if (value <= 0 || !_called) return;
            _called = false;
            _loop = StartCoroutine(Loop());
        }

        private void CallOff()
        {
            StopAllCoroutines();
            _loop = null;
            if (_shadow != null) Destroy(_shadow);
            if (_cup != null) Destroy(_cup);
            if (_decor != null) _decor.localPosition = _decorHome;
            _decor = null;
            foreach (TrailRenderer skid in _skids)
                if (skid != null) Destroy(skid.gameObject);
            _skids.Clear();
            foreach (GameObject fx in _fx) if (fx != null) Destroy(fx);
            _fx.Clear();
            foreach (int id in _blockers) PaintBlockers.Remove(id);
            _blockers.Clear();
            ClearStain();
        }

        private void ClearStain()
        {
            if (_stain != null) Destroy(_stain);
            _stain = null;
            _stainUntil = 0f;
            if (movement != null) movement.HazardMultiplier = 1f;
        }

        private void Update()
        {
            Vector3 step = transform.position - _lastPos; step.y = 0f;
            if (step.sqrMagnitude > 1e-6f) _heading = step.normalized;
            _lastPos = transform.position;

            if (_stain == null) return;
            if (Time.time >= _stainUntil) { ClearStain(); return; }
            Vector3 d = transform.position - _stainAt; d.y = 0f;
            if (movement != null) movement.HazardMultiplier = d.magnitude < radius ? stainSlow : 1f;
        }

        /// <summary>The desk accidents.</summary>
        public enum Hazard { None, Cup, Bump, Ink, Gust, Ruler }

        /// <summary>The hazards a campaign level can roll, each with its weight (0 = not yet).</summary>
        public static float Weight(Hazard h, int level, bool enemies) => h switch
        {
            Hazard.Cup => Campaign.EventAllowed(Novelty.Cup, level) ? 1f : 0f,
            Hazard.Bump => enemies && Campaign.EventAllowed(Novelty.Cup, level) ? 0.7f : 0f,
            Hazard.Ink => Campaign.EventAllowed(Novelty.Ink, level) ? 0.9f : 0f,
            Hazard.Gust => Campaign.EventAllowed(Novelty.Gust, level) ? 0.7f : 0f,
            Hazard.Ruler => Campaign.EventAllowed(Novelty.Ruler, level) ? 0.7f : 0f,
            _ => 0f,
        };

        /// <summary>On an accident's debut level, the first accident is the new one.</summary>
        public static Hazard DebutHazard(int level) => Campaign.DebutAt(level) switch
        {
            Novelty.Cup => Hazard.Cup,
            Novelty.Ink => Hazard.Ink,
            Novelty.Gust => Hazard.Gust,
            Novelty.Ruler => Hazard.Ruler,
            _ => Hazard.None,
        };

        private static readonly Hazard[] Roster = { Hazard.Cup, Hazard.Bump, Hazard.Ink, Hazard.Gust, Hazard.Ruler };

        // Weighted pick; the one just played is far less likely to come straight back.
        private Hazard Pick(int level, bool enemies)
        {
            float total = 0f;
            var w = new float[Roster.Length];
            for (int i = 0; i < Roster.Length; i++)
            {
                w[i] = Weight(Roster[i], level, enemies) * (Roster[i] == _last ? 0.25f : 1f);
                total += w[i];
            }
            float roll = Random.value * total;
            for (int i = 0; i < Roster.Length; i++)
            {
                if (roll < w[i]) return Roster[i];
                roll -= w[i];
            }
            return Hazard.Cup;
        }

        private IEnumerator Loop()
        {
            yield return new WaitForSeconds(firstDelay);
            Hazard debut = DebutHazard(PlayerProgress.LevelsPlayed);
            while (true)
            {
                var enemies = FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                Hazard h = debut != Hazard.None ? debut : Pick(PlayerProgress.LevelsPlayed, enemies.Length > 0);
                debut = Hazard.None;
                if (h == Hazard.Ink && !InkTarget(out _)) h = Hazard.Cup;
                _last = h;
                switch (h)
                {
                    case Hazard.Bump: yield return Bump(enemies); break;
                    case Hazard.Ink: yield return Ink(); break;
                    case Hazard.Gust: yield return Gust(enemies); break;
                    case Hazard.Ruler: yield return Ruler(); break;
                    default: yield return Cup(); break;
                }
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            }
        }

        // The nearest spot on an unfinished edge, a little ahead of the hero.
        private bool InkTarget(out Vector3 at)
        {
            at = default;
            EdgeNetwork net = painter != null ? painter.Network : null;
            if (net == null) return false;
            Vector3 aim = transform.position + _heading * inkLead;
            float best = float.MaxValue;
            foreach (DrawEdge e in net.Edges)
            {
                if (e.IsComplete || e.Length < 0.001f) continue;
                float t = Mathf.Clamp(e.ClosestT(aim, out float dist), 0.15f, 0.85f);
                if (dist >= best) continue;
                best = dist;
                at = e.PointAt(t);
            }
            at.y = 0.03f;
            return best < float.MaxValue;
        }

        private IEnumerator Ink()
        {
            if (!InkTarget(out Vector3 at)) yield break;
            if (hud != null) hud.Toast("INK DRIP!", "The pencil can't draw on wet ink", new Color(0.2f, 0.3f, 0.85f));
            var shadow = new GameObject("InkShadow").AddComponent<SpriteRenderer>();
            shadow.sprite = Disc();
            shadow.color = new Color(0.12f, 0.16f, 0.42f, 0.35f);
            shadow.transform.SetPositionAndRotation(at, Quaternion.Euler(90f, 0f, 0f));
            _fx.Add(shadow.gameObject);
            for (float t = 0f; t < inkWarnSeconds; t += Time.deltaTime)
            {
                shadow.transform.localScale = Vector3.one * Mathf.Lerp(inkRadius * 0.3f, inkRadius * 2f, t / inkWarnSeconds);
                yield return null;
            }
            Destroy(shadow.gameObject);
            Haptics.Light();

            var blot = new GameObject("InkBlot").AddComponent<SpriteRenderer>();
            blot.sprite = Splat();
            blot.color = inkColor;
            blot.transform.SetPositionAndRotation(at, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            _fx.Add(blot.gameObject);
            int id = PaintBlockers.Add(at, inkRadius);
            _blockers.Add(id);
            float full = inkRadius * 2.3f;   // the splat's drops reach past the wet core
            for (float t = 0f; t < inkSeconds; t += Time.deltaTime)
            {
                float pop = Mathf.Clamp01(t / 0.12f);
                blot.transform.localScale = Vector3.one * (full * (pop < 1f ? Mathf.Lerp(0.4f, 1.1f, pop) : 1f));
                float fade = Mathf.Clamp01((inkSeconds - t) / 0.6f);
                Color c = inkColor; c.a *= fade;
                blot.color = c;
                yield return null;
            }
            PaintBlockers.Remove(id);
            _blockers.Remove(id);
            _fx.Remove(blot.gameObject);
            Destroy(blot.gameObject);
        }

        private IEnumerator Gust(EnemyFollow[] enemies)
        {
            Vector2 flat = Random.insideUnitCircle.normalized;
            Vector3 dir = flat != Vector2.zero ? new Vector3(flat.x, 0f, flat.y) : Vector3.right;
            if (hud != null) hud.Toast("FAN GUST!", "The wind pushes everything", new Color(0.35f, 0.7f, 0.9f));
            Haptics.Light();
            foreach (EnemyFollow e in enemies)
                if (e != null && e.isActiveAndEnabled) e.Slide(dir, gustSpeed * gustSeconds * 0.4f, gustSeconds);

            Vector3 side = Vector3.Cross(Vector3.up, dir);
            float nextStreak = 0f;
            for (float t = 0f; t < gustSeconds; t += Time.deltaTime)
            {
                PullZones.Add(dir * (gustSpeed * Mathf.Sin(Mathf.PI * t / gustSeconds)));
                if (t >= nextStreak)
                {
                    nextStreak = t + 0.08f;
                    Vector3 from = transform.position - dir * 9f + side * Random.Range(-7f, 7f);
                    from.y = Random.Range(0.25f, 1.4f);
                    StartCoroutine(Streak(from, dir));
                }
                yield return null;
            }
        }

        // One wind line flying downwind across the page.
        private IEnumerator Streak(Vector3 from, Vector3 dir)
        {
            if (_windMaterial == null)
                _windMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Auto_WindStreak", renderQueue = 3050 };
            var go = new GameObject("WindStreak");
            go.transform.position = from;
            var tr = go.AddComponent<TrailRenderer>();
            tr.sharedMaterial = _windMaterial;
            tr.time = 0.3f;
            tr.minVertexDistance = 0.1f;
            tr.widthMultiplier = 0.1f;
            tr.startColor = new Color(0.45f, 0.58f, 0.8f, 0.85f);
            tr.endColor = new Color(0.45f, 0.58f, 0.8f, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tr.receiveShadows = false;
            _fx.Add(go);
            for (float t = 0f; t < 1.3f && go != null; t += Time.deltaTime)
            {
                go.transform.position += dir * (16f * Time.deltaTime);
                yield return null;
            }
            _fx.Remove(go);
            if (go != null) Destroy(go);
        }

        private IEnumerator Ruler()
        {
            if (rulerPrefab == null) { yield return Cup(); yield break; }
            Vector2 flat = Random.insideUnitCircle.normalized;
            Vector3 dir = flat != Vector2.zero ? new Vector3(flat.x, 0f, flat.y) : Vector3.forward;
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            Vector3 center = transform.position; center.y = 0f;
            float halfLength = 3.5f * rulerScale;
            const float halfThick = 0.35f, travel = 15f;
            if (hud != null) hud.Toast("RULER SWEEP!", "Get out of the red lane", new Color(0.9f, 0.3f, 0.25f));

            // The lane it will sweep, flashing.
            var lane = new GameObject("RulerLane").AddComponent<SpriteRenderer>();
            lane.sprite = Square();
            lane.transform.SetPositionAndRotation(center + Vector3.up * 0.03f, Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f));
            lane.transform.localScale = new Vector3(halfLength * 2f, travel * 2f, 1f);
            _fx.Add(lane.gameObject);
            for (float t = 0f; t < rulerWarnSeconds; t += Time.deltaTime)
            {
                float flash = 0.5f + 0.5f * Mathf.Sin(t / rulerWarnSeconds * Mathf.PI * 7f);
                lane.color = new Color(0.95f, 0.2f, 0.15f, Mathf.Lerp(0.12f, 0.32f, flash));
                yield return null;
            }

            // Stood on its long edge and pushed across.
            GameObject ruler = Instantiate(rulerPrefab);
            foreach (Collider c in ruler.GetComponentsInChildren<Collider>()) Destroy(c);
            ruler.transform.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-90f, 0f, 0f);
            ruler.transform.localScale = Vector3.one * rulerScale;
            _fx.Add(ruler);
            var hitEnemies = new HashSet<EnemyFollow>();
            bool heroHit = false;
            var enemies = FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            Haptics.Light();
            for (float s = -travel; s <= travel; s += rulerSpeed * Time.deltaTime)
            {
                Vector3 at = center + dir * s;
                ruler.transform.position = at + Vector3.up * (0.55f * rulerScale);
                lane.color = new Color(0.95f, 0.2f, 0.15f, 0.2f);
                if (!heroHit && Inside(transform.position - at, dir, side, halfLength, halfThick))
                {
                    heroHit = true;
                    if (combat != null) combat.TakeDamage(1);
                    if (knockback != null) knockback.ApplyKnockback(dir, rulerKnockback);
                    Haptics.Medium();
                    if (_loop == null) yield break;   // that hit ended the level
                }
                foreach (EnemyFollow e in enemies)
                    if (e != null && e.isActiveAndEnabled && !hitEnemies.Contains(e)
                        && Inside(e.transform.position - at, dir, side, halfLength, halfThick))
                    {
                        hitEnemies.Add(e);
                        e.Slide(dir, 3f, 0.45f);
                    }
                yield return null;
            }
            _fx.Remove(ruler);
            _fx.Remove(lane.gameObject);
            Destroy(ruler);
            Destroy(lane.gameObject);
        }

        private static bool Inside(Vector3 d, Vector3 dir, Vector3 side, float halfLength, float halfThick)
            => Mathf.Abs(Vector3.Dot(d, dir)) < halfThick + 0.3f && Mathf.Abs(Vector3.Dot(d, side)) < halfLength + 0.2f;

        private IEnumerator Cup()
        {
            Vector3 at = transform.position + _heading * leadMeters;
            at.y = 0.03f;
            var shadow = new GameObject("CupShadow").AddComponent<SpriteRenderer>();
            shadow.sprite = Disc();
            shadow.color = new Color(0.12f, 0.16f, 0.42f, 0.35f);
            shadow.transform.SetPositionAndRotation(at, Quaternion.Euler(90f, 0f, 0f));
            _shadow = shadow.gameObject;
            var cup = new GameObject("CoffeeCup").AddComponent<SpriteRenderer>();
            cup.sprite = cupSprite;
            _cup = cup.gameObject;
            float cupSize = cupSprite != null ? Mathf.Max(cupSprite.bounds.size.x, cupSprite.bounds.size.y) : 1f;
            cup.transform.localScale = Vector3.one * (radius * 2f / Mathf.Max(0.01f, cupSize));
            Camera cam = Camera.main;
            if (cam != null) cup.transform.rotation = cam.transform.rotation;

            const float dropSeconds = 0.35f;
            for (float t = 0f; t < warnSeconds; t += Time.deltaTime)
            {
                float k = t / warnSeconds;
                shadow.transform.localScale = Vector3.one * Mathf.Lerp(radius * 0.4f, radius * 2f, k);
                float drop = Mathf.Clamp01((t - (warnSeconds - dropSeconds)) / dropSeconds);
                cup.enabled = drop > 0f;
                cup.transform.position = at + Vector3.up * Mathf.Lerp(14f, 0.4f, drop * drop);
                yield return null;
            }
            Destroy(shadow.gameObject);
            Destroy(cup.gameObject);

            Vector3 d = transform.position - at; d.y = 0f;
            if (d.magnitude < radius && combat != null) combat.TakeDamage(1);
            Haptics.Medium();
            if (_loop == null) yield break;   // that hit ended the level

            ClearStain();
            var stain = new GameObject("CoffeeStain").AddComponent<SpriteRenderer>();
            stain.sprite = stainSprite;
            float size = stainSprite != null ? Mathf.Max(stainSprite.bounds.size.x, stainSprite.bounds.size.y) : 1f;
            stain.transform.SetPositionAndRotation(at, Quaternion.Euler(90f, Random.Range(0f, 360f), 0f));
            stain.transform.localScale = Vector3.one * (radius * 2.4f / Mathf.Max(0.01f, size));
            _stain = stain.gameObject;
            _stainAt = at;
            _stainUntil = Time.time + stainSeconds;
        }

        private IEnumerator Bump(EnemyFollow[] enemies)
        {
            Vector2 flat = Random.insideUnitCircle.normalized;
            Vector3 direction = flat != Vector2.zero ? new Vector3(flat.x, 0f, flat.y) : Vector3.forward;

            if (hud != null) hud.Toast("TABLE BUMP!", "Everything slides", new Color(0.95f, 0.4f, 0.15f));
            Haptics.Medium();
            TableBumped?.Invoke(direction);
            // The hero is on the same desk: it skids the same way, a little less far.
            if (knockback != null) knockback.Slide(direction, bumpPush * heroSlideMultiplier, slideSeconds);

            // One shove, one direction: all of them skid together, each leaving a mark on the page.
            var sliding = new List<EnemyFollow>();
            foreach (EnemyFollow e in enemies)
            {
                if (e == null || !e.isActiveAndEnabled) continue;
                e.Slide(direction, bumpPush, slideSeconds);
                sliding.Add(e);
                _skids.Add(NewSkid(e.transform));
            }

            // The desk jolts along the bump and settles back.
            _decor = painter != null && painter.Network != null ? FindDecor() : null;
            _decorHome = _decor != null ? _decor.localPosition : Vector3.zero;
            for (float t = 0f; t < slideSeconds; t += Time.deltaTime)
            {
                float k = t / slideSeconds;
                if (_decor != null) _decor.localPosition = _decorHome + direction * (decorJolt * Mathf.Sin(k * Mathf.PI * 3f) * (1f - k));
                for (int i = 0; i < sliding.Count; i++)
                    if (sliding[i] != null) _skids[i].transform.position = OnPage(sliding[i].transform.position);
                yield return null;
            }
            if (_decor != null) _decor.localPosition = _decorHome;
            _decor = null;

            foreach (TrailRenderer skid in _skids)
                if (skid != null) Destroy(skid.gameObject, skid.time);
            _skids.Clear();
        }

        private static Vector3 OnPage(Vector3 position) { position.y = 0.04f; return position; }

        private TrailRenderer NewSkid(Transform enemy)
        {
            if (_skidMaterial == null)
                _skidMaterial = new Material(Shader.Find("Sprites/Default")) { name = "Auto_BumpSkid", renderQueue = 3050 };
            var go = new GameObject("BumpSkid");
            go.transform.SetPositionAndRotation(OnPage(enemy.position), Quaternion.Euler(90f, 0f, 0f));
            var skid = go.AddComponent<TrailRenderer>();
            skid.sharedMaterial = _skidMaterial;
            skid.alignment = LineAlignment.TransformZ;
            skid.time = 0.9f;
            skid.minVertexDistance = 0.05f;
            skid.numCapVertices = 3;
            skid.widthMultiplier = 0.32f * enemy.lossyScale.x;
            skid.widthCurve = AnimationCurve.Linear(0f, 0.25f, 1f, 1f);
            skid.startColor = new Color(skidColor.r, skidColor.g, skidColor.b, 0f);
            skid.endColor = skidColor;
            skid.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            skid.receiveShadows = false;
            return skid;
        }

        private Transform FindDecor()
        {
            foreach (SpriteRenderer s in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (s.transform.parent != null && s.transform.parent.name == "Decor") return s.transform.parent;
            return null;
        }

        private static Sprite _square, _splat;

        private static Sprite Square()
        {
            if (_square != null) return _square;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Point };
            var white = new Color[16];
            for (int i = 0; i < 16; i++) white[i] = Color.white;
            tex.SetPixels(white);
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4);   // 1 unit wide
            return _square;
        }

        // An ink splat: a round blot with a few drops thrown around it, 1 unit wide.
        private static Sprite Splat()
        {
            if (_splat != null) return _splat;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var drops = new List<Vector3>();   // x, y, radius in pixels
            drops.Add(new Vector3(n * 0.5f, n * 0.5f, n * 0.3f));
            var rng = new System.Random(208);
            for (int i = 0; i < 9; i++)
            {
                double a = rng.NextDouble() * Mathf.PI * 2f;
                float d = n * (0.27f + (float)rng.NextDouble() * 0.17f);
                float r = n * (0.04f + (float)rng.NextDouble() * 0.06f);
                drops.Add(new Vector3(n * 0.5f + Mathf.Cos((float)a) * d, n * 0.5f + Mathf.Sin((float)a) * d, r));
            }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float alpha = 0f;
                    foreach (Vector3 drop in drops)
                        alpha = Mathf.Max(alpha, Mathf.Clamp01(drop.z - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(drop.x, drop.y))));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            tex.Apply();
            _splat = Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f, n);
            return _splat;
        }

        private static Sprite _disc;
        private static Sprite Disc()
        {
            if (_disc != null) return _disc;
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)))));
            tex.Apply();
            _disc = Sprite.Create(tex, new Rect(0, 0, n, n), Vector2.one * 0.5f, n);   // 1 unit wide
            return _disc;
        }
    }
}
