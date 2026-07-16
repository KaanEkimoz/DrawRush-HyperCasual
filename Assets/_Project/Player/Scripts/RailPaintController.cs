using System;
using System.Collections.Generic;
using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Edge-painting movement. Touching an anchor sphere attaches the player to that
    /// anchor; pushing the stick toward a neighbor selects that edge and the player
    /// slides along the straight line between the two spheres, painting the span it
    /// covers — the paint is persistent (see <see cref="DrawEdge"/> / <see cref="EdgeFill"/>).
    /// Reaching the far sphere re-anchors there so the next edge can be chosen. An enemy
    /// touch calls <see cref="Detach"/>, freeing the player to flee (paint is kept); touching
    /// any sphere again re-attaches and resumes from where that side left off.
    ///
    /// Runs before ThirdPersonMovement so the movement lock applies in the same physics step
    /// (no one-frame leak of free movement).
    /// </summary>
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class RailPaintController : MonoBehaviour
    {
        [Header("Refs (auto-resolved from this GameObject if empty)")]
        [SerializeField] private PlayerInteract interact;
        [SerializeField] private ThirdPersonMovement movement;
        [SerializeField] private PlayerKnockback knockback;

        [Header("Tuning")]
        [Tooltip("Minimum input magnitude before an edge is picked or driven.")]
        [SerializeField] private float inputDeadzone = 0.3f;
        [Tooltip("Minimum alignment (dot) between input and an edge direction to select it.")]
        [SerializeField] private float selectThreshold = 0.4f;
        [Tooltip("Rail slide speed. When <= 0, falls back to GameConfig.playerSpeed.")]
        [SerializeField] private float railSpeed = 0f;
        [Tooltip("World-space distance to the far anchor that counts as arrival and completes " +
                 "the edge — independent of the anchor's trigger AND of input (the player may " +
                 "release the stick right at the end).")]
        [SerializeField] private float arrivalDistance = 0.6f;

        // While actively drawing, only snap-complete once the player is essentially on the far
        // drop, so the last stretch of an arc is not chopped off (which left the body beside the
        // finished line near the end). The larger arrivalDistance still applies when the stick is
        // released. A FixedUpdate step is ~0.05 world units, well under this, so it never skips.
        private const float ArrivalSnap = 0.15f;

        // How much of the stick must point along the rail before it counts as "go". Below this the
        // player is pushing across the rail, where the forward/back sign is ambiguous and would
        // jitter. 0.2 ≈ within 78° of the rail axis, so any sane input still reads as intent.
        private const float AlignmentDeadzone = 0.2f;

        private CharacterController _characterController;
        private EdgeNetwork _network;
        private DrawPart _currentPart;
        private DrawPart _targetPart;
        private DrawEdge _edge;
        private float _localT;          // 0 = at current anchor, 1 = at target anchor
        private bool _detached;
        private readonly List<DrawEdge> _edgeBuffer = new();

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (interact == null) interact = GetComponent<PlayerInteract>();
            if (movement == null) movement = GetComponent<ThirdPersonMovement>();
            if (knockback == null) knockback = GetComponent<PlayerKnockback>();
        }

        private void OnEnable()
        {
            if (interact != null) interact.PartTouched += OnPartTouched;
        }

        private void OnDisable()
        {
            if (interact != null) interact.PartTouched -= OnPartTouched;
        }

        /// <summary>Releases the paint rail so the player moves freely — used both when an
        /// edge finishes (get off the rail) and on enemy contact (escape). Paint progress is
        /// preserved; touching a sphere again re-attaches. Clears the current anchor so the
        /// player is not immediately re-locked while still standing in a sphere's trigger.</summary>
        public void Detach()
        {
            _detached = true;
            _edge = null;
            _targetPart = null;
            _currentPart = null;
            _localT = 0f;
            if (movement != null) movement.MovementLocked = false;
        }

        private void OnPartTouched(DrawPart part)
        {
            // Reached the far end of the edge being painted → finish that edge and free the
            // player (get off the rail). The whole span fills; if it was the last unpainted
            // edge, EdgeNetwork raises AllCompleted and the level ends. We do NOT re-anchor
            // here — that was the bug that re-locked the player onto a new rail.
            if (_edge != null && part == _targetPart)
            {
                _edge.PaintFrom(_currentPart, _currentPart == _edge.A ? 1f : 0f);
                Detach();
                return;
            }

            // Every edge owns its own drops, so a corner holds two of them a drop-gap apart:
            // painting toward one, the player clips the NEIGHBOUR edge's trigger first. Falling
            // through to the re-anchor below would silently discard an almost-finished edge —
            // it freezes at ~95%, AllCompleted never fires, and the player has to walk back and
            // repaint it. Past the halfway mark the edge is committed, so ignore foreign drops
            // and let the arrival check in FixedUpdate finish it properly. Near the start,
            // re-picking a different edge from the same corner is still legitimate.
            if (_edge != null && _localT >= 0.5f) return;

            // Fresh attach, or re-pick from the same corner: lock to this anchor and wait for
            // a stick direction to choose an edge.
            _currentPart = part;
            _targetPart = null;
            _edge = null;
            _localT = 0f;
            _detached = false;
            if (_network == null || !_network.isActiveAndEnabled) _network = ResolveNetwork(part);
        }

        private void FixedUpdate()
        {
            if (interact == null) return;

            // Knockback owns movement for its brief duration — don't touch MovementLocked
            // or try to drive the rail until it's done.
            if (knockback != null && knockback.IsActive) return;

            // Not painting: no anchor visited yet, or detached to flee.
            if (_currentPart == null || _detached)
            {
                if (movement != null) movement.MovementLocked = false;
                return;
            }

            if (movement != null) movement.MovementLocked = true;

            Vector3 worldInput = ResolveWorldInput();
            bool released = worldInput.sqrMagnitude < 0.0001f;

            // Arrival completion — finish the edge + get off the rail. Two cases, so an ACTIVE
            // draw is not chopped short:
            //  • While the stick is held, only snap once the player is essentially ON the far
            //    drop (ArrivalSnap). The old code snapped the whole last `arrivalDistance` (0.6)
            //    the moment the player got that close — on a STRAIGHT edge that just looked like
            //    stopping a touch early, but on an ARC the finished line curved on ~0.6 further
            //    while the body stayed behind, so the character ended up beside the line near the
            //    end (Kaan's bug).
            //  • If the stick is released, keep the generous `arrivalDistance` so letting go right
            //    before the drop still completes the edge.
            if (_edge != null && _targetPart != null)
            {
                Vector3 pp = transform.position; pp.y = 0f;
                Vector3 tp = _targetPart.Transform.position; tp.y = 0f;
                float sqr = (pp - tp).sqrMagnitude;
                if (sqr <= ArrivalSnap * ArrivalSnap ||
                    (released && sqr <= arrivalDistance * arrivalDistance))
                {
                    _edge.PaintFrom(_currentPart, _currentPart == _edge.A ? 1f : 0f);
                    Detach();
                    return;
                }
            }

            if (released) return;   // no input → hold position

            if (_edge == null && !TrySelectEdge(worldInput)) return;

            // Geometry-agnostic traversal: works for straight edges AND arcs. _localT is the
            // local progress current→target; convert to the edge's A→B parameter (edgeT). All
            // positions/directions/length come from the edge, so the player slides along the
            // arc exactly like the painted line and wall.
            float len = _edge.Length;
            if (len < 0.001f) { _edge = null; _targetPart = null; return; }

            float edgeTNow = _currentPart == _edge.A ? _localT : 1f - _localT;
            // Heading along current→target: TangentAt is in the A→B sense; flip if we entered from B.
            Vector3 heading = _currentPart == _edge.A ? _edge.TangentAt(edgeTNow) : -_edge.TangentAt(edgeTNow);

            // The stick picks WHICH WAY along the rail — never how fast. Dot(worldInput, heading)
            // used to fold in both the stick's deflection AND cos(angle to the rail), so a
            // half-pushed or off-axis stick crawled, and on an arc the heading turns under the
            // player while they hold a fixed direction — so the speed sagged and recovered by
            // itself mid-curve. Normalising first makes it purely a direction decision.
            float alignment = Vector3.Dot(worldInput.normalized, heading);   // -1..1
            if (Mathf.Abs(alignment) < AlignmentDeadzone) return;            // pushed across the rail, not along it
            float along = Mathf.Sign(alignment);
            float speed = railSpeed > 0f
                ? railSpeed
                : (GameServices.Config != null ? GameServices.Config.playerSpeed : 2.7f);
            _localT = Mathf.Clamp01(_localT + along * speed * Time.deltaTime / len);   // arc-length normalized

            float edgeT = _currentPart == _edge.A ? _localT : 1f - _localT;
            _edge.PaintFrom(_currentPart, edgeT);

            // Edge done — the two painted spans met in the middle (arrival at the far anchor
            // is handled above, before the input gate).
            if (_edge.IsComplete) { Detach(); return; }

            Vector3 targetPos = _edge.PointAt(edgeT);
            Vector3 delta = targetPos - transform.position;
            delta.y = 0f;
            _characterController.Move(delta);

            // Re-evaluate heading at the new point so the player visibly curves along the arc.
            // (No magnitude check needed any more — below the deadzone we already returned.)
            Vector3 headNow = _currentPart == _edge.A ? _edge.TangentAt(edgeT) : -_edge.TangentAt(edgeT);
            transform.rotation = Quaternion.LookRotation(headNow * along, Vector3.up);
        }

        // Pick the authored edge touching the current anchor that is best aligned with the
        // stick, and slide along it. Edges come from EdgeNetwork (authored Kenar prefabs), so
        // there is no neighbor graph to consult.
        private bool TrySelectEdge(Vector3 worldInput)
        {
            if (_network == null || !_network.isActiveAndEnabled) _network = ResolveNetwork(_currentPart);
            if (_network == null) return false;

            _network.GetEdgesTouching(_currentPart, _edgeBuffer);

            DrawEdge best = null;
            DrawPart bestTarget = null;
            float bestDot = selectThreshold;
            for (int i = 0; i < _edgeBuffer.Count; i++)
            {
                DrawEdge e = _edgeBuffer[i];
                DrawPart other = e.Other(_currentPart);
                if (other == null) continue;

                // Leave the anchor along the edge's tangent there (matters for arcs, whose
                // initial direction differs from the straight chord). For straight edges this
                // equals the old endpoint-to-endpoint direction.
                Vector3 dir = _currentPart == e.A ? e.TangentAt(0f) : -e.TangentAt(1f);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;

                float dot = Vector3.Dot(worldInput, dir.normalized);
                if (dot > bestDot) { bestDot = dot; best = e; bestTarget = other; }
            }

            if (best == null) return false;
            _edge = best;
            _targetPart = bestTarget;
            _localT = 0f;
            return true;
        }

        private Vector3 ResolveWorldInput()
        {
            Vector2 input = movement != null ? movement.MoveInput : Vector2.zero;
            var raw = new Vector3(input.x, 0f, input.y);
            if (raw.magnitude < inputDeadzone) return Vector3.zero;

            Transform cam = GameServices.MainCamera;
            float cameraY = cam != null ? cam.eulerAngles.y : 0f;
            float angle = Mathf.Atan2(raw.x, raw.z) * Mathf.Rad2Deg + cameraY;
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        // Scope to the touched part's level group in the mega-scene (like DrawPart does),
        // so the player binds to the active level's EdgeNetwork. Falls back to a scene-wide
        // search for the legacy scene-per-level layout.
        private static EdgeNetwork ResolveNetwork(DrawPart part)
        {
            Transform t = part.Transform;
            while (t != null && !t.name.StartsWith("Level_", StringComparison.Ordinal))
                t = t.parent;

            if (t != null)
            {
                EdgeNetwork scoped = t.GetComponentInChildren<EdgeNetwork>();
                if (scoped != null) return scoped;
            }
            return UnityEngine.Object.FindFirstObjectByType<EdgeNetwork>();
        }
    }
}
