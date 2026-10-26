using System;
using System.Collections;
using System.Collections.Generic;
using DrawRush.Core;
using DrawRush.Player;
using UnityEngine;

namespace DrawRush.Drawing
{
    /// <summary>
    /// Free drawing: wherever the player walks within a tolerance band of the level's ghost
    /// shape, that stretch of the shape is painted (snapped onto the template, so the drawing
    /// always looks clean). No rails: the player can leave, dodge and rejoin anywhere.
    ///
    /// Forgiveness, from the redesign research: fresh paint stays PENDING for a moment and only
    /// an enemy hit in that window can wipe it; an edge painted all but a sliver finishes itself;
    /// tiny gaps close; once the whole shape is 99% painted the last 1% (a margin of error, Kaan's
    /// call after playtesting — 90% let levels end with whole edges unpainted) is swept in and the
    /// level is won. Late in a level the band widens a little if the
    /// player stalls, and the remaining gaps are highlighted.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class FreeDrawPainter : MonoBehaviour
    {
        [Header("Band")]
        [Tooltip("Max ground distance (m) from the template that still paints.")]
        [SerializeField] private float band = 0.5f;
        [Tooltip("Extra band on arcs, as a multiple (curves are harder to follow).")]
        [SerializeField] private float arcBandScale = 1.25f;
        [Tooltip("A jump longer than this (m) along an edge between two steps does not fill the " +
                 "stretch in between (knockback, teleport) — only normal walking connects.")]
        [SerializeField] private float maxStepMeters = 1.2f;

        [Header("Forgiveness")]
        [Tooltip("Seconds fresh paint stays wipeable by an enemy hit before it is kept for good.")]
        [SerializeField] private float pendingSeconds = 1.2f;
        [Tooltip("An edge this painted (0-1) completes itself.")]
        [SerializeField, Range(0.5f, 1f)] private float edgeAutoComplete = 0.99f;
        [Tooltip("Unpainted gaps shorter than this (m) close on their own.")]
        [SerializeField] private float microGapMeters = 0.3f;
        [Tooltip("A sliver this short (m) left at an edge's end closes on its own: it sits under the " +
                 "corner drop, where the shapes' edges crowd together.")]
        [SerializeField] private float endGapMeters = 0.6f;
        [Tooltip("Whole-shape coverage (0-1) at which the rest is swept in and the level is won.")]
        [SerializeField, Range(0.5f, 1f)] private float winCoverage = 0.99f;
        [Tooltip("Seconds the finishing sweep takes to paint what is left.")]
        [SerializeField] private float sweepSeconds = 0.6f;

        [Header("Late-level help")]
        [Tooltip("Coverage (0-1) after which the remaining gaps are highlighted.")]
        [SerializeField, Range(0f, 1f)] private float highlightFrom = 0.7f;
        [Tooltip("Coverage (0-1) after which a stall widens the band.")]
        [SerializeField, Range(0f, 1f)] private float magnetFrom = 0.8f;
        [Tooltip("Seconds without new paint that count as a stall.")]
        [SerializeField] private float stallSeconds = 4f;
        [Tooltip("Band multiple while the stall magnet is on.")]
        [SerializeField] private float stallBandScale = 1.6f;

        [Tooltip("Pending paint this close (m) to the player cannot be eaten: an Eater only eats the " +
                 "trail left behind, so one hugging the player cannot stall the level.")]
        [SerializeField] private float brushProtectRadius = 1f;

        [SerializeField] private PlayerCombat combat;

        /// <summary>Whole-shape painted fraction (0-1), length-weighted.</summary>
        public float Coverage { get; private set; }

        /// <summary>Raised when <see cref="Coverage"/> changes.</summary>
        public event Action<float> CoverageChanged;

        /// <summary>Raised when an enemy hit wiped pending paint.</summary>
        public event Action PaintLost;

        /// <summary>Raised when a level (or a restart of it) starts being tracked.</summary>
        public event Action<EdgeNetwork> LevelBound;

        /// <summary>The shape being drawn now (null between levels).</summary>
        public EdgeNetwork Network => _network;

        /// <summary>Extra band multiple from boosters (1 = none). Set by the booster system.</summary>
        public float BandBoost { get; set; } = 1f;

        /// <summary>True on steps where the brush laid paint on the shape (for the brush glow).</summary>
        public bool IsPainting { get; private set; }

        /// <summary>Band currently in effect for straight edges (m).</summary>
        public float CurrentBand => band * BandBoost * (_stallMagnet ? stallBandScale : 1f);

        private EdgeNetwork _network;
        private int _generation = -1;
        private readonly Dictionary<DrawEdge, float> _lastT = new();
        private float _lastPaintTime;
        private bool _stallMagnet, _sweeping, _highlighting;
        private Coroutine _sweep;

        private void Awake()
        {
            if (combat == null) combat = GetComponent<PlayerCombat>();
        }

        private void OnEnable()
        {
            if (combat != null) combat.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (combat != null) combat.Damaged -= OnDamaged;
        }

        /// <summary>Start a level fresh: bind to its network and forget per-edge progress.</summary>
        public void Bind(EdgeNetwork network)
        {
            if (_sweep != null) StopCoroutine(_sweep);
            _sweep = null;
            _network = network;
            _generation = network != null ? network.Generation : -1;
            _lastT.Clear();
            _sweeping = false;
            _stallMagnet = false;
            _highlighting = false;
            _lastPaintTime = Time.time;
            Coverage = -1f;   // force the event so listeners reset even from 0
            SetCoverage(0f);
            LevelBound?.Invoke(network);
        }

        private void FixedUpdate()
        {
            // A new level, or the same one restarted (its network rebuilds and bumps Generation).
            if (_network == null || !_network.isActiveAndEnabled || _network.Generation != _generation)
            {
                EdgeNetwork found = _network != null && _network.isActiveAndEnabled ? _network : FindActiveNetwork();
                if (found == null) return;
                Bind(found);
            }
            var state = GameServices.State;
            IsPainting = false;
            if (_sweeping || (state != null && state.IsGameWon) || _network.IsComplete) return;

            Vector3 p = transform.position;
            float now = Time.time;
            bool painted = false;
            IReadOnlyList<DrawEdge> edges = _network.Edges;
            for (int i = 0; i < edges.Count; i++)
            {
                DrawEdge e = edges[i];
                if (e.IsComplete) continue;
                float len = e.Length;
                if (len < 0.001f) continue;

                float t = e.ClosestT(p, out float dist);
                float limit = CurrentBand * (e.IsArc ? arcBandScale : 1f);
                if (dist > limit) { _lastT.Remove(e); continue; }

                float from = _lastT.TryGetValue(e, out float last) && Mathf.Abs(t - last) * len <= maxStepMeters ? last : t;
                // A single step still lays a little paint so standing on the line shows a mark.
                float pad = 0.05f / len;
                e.PaintPending(Mathf.Min(from, t) - pad, Mathf.Max(from, t) + pad, now);
                _lastT[e] = t;
                painted = true;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                DrawEdge e = edges[i];
                if (e.IsComplete) continue;
                e.ConfirmPendingOlderThan(now - pendingSeconds);
                // Finishing an edge is a save point: the moment it is all but painted it completes,
                // fresh paint included, instead of waiting out the pending window first (that wait
                // made finished edges light up a second or more after the player got there).
                if (e.CoverageClosingGaps(microGapMeters, endGapMeters) >= edgeAutoComplete) { e.CompleteAll(); continue; }
                if (!e.Fill.HasPending)
                {
                    e.CloseGaps(microGapMeters, endGapMeters);
                    if (e.Fill.Coverage >= edgeAutoComplete) e.CompleteAll();
                }
            }

            IsPainting = painted;
            if (painted) _lastPaintTime = now;
            float cov = _network.Coverage;
            SetCoverage(cov);

            _stallMagnet = cov >= magnetFrom && now - _lastPaintTime >= stallSeconds;
            bool highlight = cov >= highlightFrom;
            if (highlight != _highlighting) { _highlighting = highlight; _network.SetGapHighlight(highlight); }

            if (cov >= winCoverage) _sweep = StartCoroutine(Sweep());
        }

        // Paint what is left, edge by edge, quickly — the last completion fires the win.
        private IEnumerator Sweep()
        {
            _sweeping = true;
            var left = new List<DrawEdge>();
            foreach (DrawEdge e in _network.Edges) if (!e.IsComplete) left.Add(e);
            float step = left.Count > 0 ? sweepSeconds / left.Count : 0f;
            foreach (DrawEdge e in left)
            {
                e.ConfirmAll();
                e.CompleteAll();
                SetCoverage(_network.Coverage);
                if (step > 0f) yield return new WaitForSeconds(step);
            }
            SetCoverage(1f);
            _sweep = null;
        }

        /// <summary>Wipe not-yet-kept paint within <paramref name="radius"/> m of
        /// <paramref name="worldPos"/> (a paint-eating enemy walking over it).</summary>
        public void ErasePendingNear(Vector3 worldPos, float radius)
        {
            if (_network == null || _sweeping) return;
            Vector3 toPlayer = worldPos - transform.position; toPlayer.y = 0f;
            if (toPlayer.magnitude < brushProtectRadius) return;   // wet paint under the brush is safe
            bool lost = false;
            foreach (DrawEdge e in _network.Edges)
            {
                if (e.IsComplete || !e.Fill.HasPending) continue;
                float len = e.Length;
                if (len < 0.001f) continue;
                float t = e.ClosestT(worldPos, out float dist);
                if (dist > radius) continue;
                float half = Mathf.Sqrt(radius * radius - dist * dist) / len;
                lost |= e.ErasePending(t - half, t + half);
            }
            if (!lost) return;
            SetCoverage(_network.Coverage);
            if (Time.time - _lastLossFlash > 0.5f) { _lastLossFlash = Time.time; PaintLost?.Invoke(); }
        }

        private float _lastLossFlash = -1f;

        private void OnDamaged()
        {
            if (_network == null || _sweeping) return;
            bool lost = false;
            foreach (DrawEdge e in _network.Edges) lost |= e.ClearPending();
            _lastT.Clear();
            if (lost) { SetCoverage(_network.Coverage); PaintLost?.Invoke(); }
        }

        private void SetCoverage(float value)
        {
            if (Mathf.Approximately(value, Coverage)) return;
            Coverage = value;
            CoverageChanged?.Invoke(value);
        }

        private static EdgeNetwork FindActiveNetwork() => FindFirstObjectByType<EdgeNetwork>(FindObjectsInactive.Exclude);
    }
}
