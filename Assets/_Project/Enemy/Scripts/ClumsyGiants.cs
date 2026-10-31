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
    /// hazard: a coffee cup lands (its shadow warns first; the hit hurts and the spill slows you)
    /// or the table gets bumped (every enemy skids the same way, the desk jolts). Never during
    /// the opening seconds of a level, and only from a set level on. Nothing outlives the level:
    /// a win or the hero's death calls off whatever is still in the air, and a revive starts over.
    /// </summary>
    public sealed class ClumsyGiants : MonoBehaviour
    {
        /// <summary>The table was bumped; carries the ground direction everything slides.</summary>
        public static event Action<Vector3> TableBumped;

        [SerializeField] private FreeDrawPainter painter;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private ThirdPersonMovement movement;
        [SerializeField] private BoosterHud hud;

        [Header("Timing")]
        [Tooltip("First campaign level (1-based) with hazards.")]
        [SerializeField] private int fromLevel = 8;
        [SerializeField] private float firstDelay = 7f;
        [SerializeField] private Vector2 interval = new(9f, 14f);
        [Tooltip("Chance a hazard is a table bump instead of a cup, when enemies are around.")]
        [SerializeField, Range(0f, 1f)] private float bumpChance = 0.3f;

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
        [Tooltip("How far (m) the desk decor jolts along the bump.")]
        [SerializeField] private float decorJolt = 0.45f;
        [SerializeField] private Color skidColor = new(0.12f, 0.16f, 0.42f, 0.45f);

        private Coroutine _loop;
        private Vector3 _lastPos, _heading;
        private GameObject _stain;
        private Vector3 _stainAt;
        private float _stainUntil;
        private GameObject _shadow, _cup;
        private Transform _decor;
        private Vector3 _decorHome;
        private readonly List<TrailRenderer> _skids = new();
        private static Material _skidMaterial;
        private GameState _state;
        private PlayerHealth _health;
        private bool _called;   // hazards were running when the level ended

        private void Awake()
        {
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
            _loop = PlayerProgress.LevelsPlayed + 1 >= fromLevel ? StartCoroutine(Loop()) : null;
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

        private IEnumerator Loop()
        {
            yield return new WaitForSeconds(firstDelay);
            while (true)
            {
                var enemies = FindObjectsByType<EnemyFollow>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                if (enemies.Length > 0 && Random.value < bumpChance) yield return Bump(enemies);
                else yield return Cup();
                yield return new WaitForSeconds(Random.Range(interval.x, interval.y));
            }
        }

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
