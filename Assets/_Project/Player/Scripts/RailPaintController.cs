using System;
using System.Collections.Generic;
using UnityEngine;
using DrawRush.Core;
using DrawRush.Drawing;

namespace DrawRush.Player
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

        [Tooltip("How far (world units) the player must have painted away from the start drop " +
                 "before sliding back onto it lets them off the rail. The guard keeps the drop from " +
                 "becoming a free in-and-out escape hatch: wiggling inside the drop never releases.")]
        [SerializeField] private float escapeArmDistance = 1f;

        [Tooltip("Degrees per second the player turns to face the edge that still needs painting " +
                 "while anchored and undecided. Snapping instantly reads as a glitch.")]
        [SerializeField] private float aimTurnSpeed = 540f;

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
        private float _farthestAlong;   // world units painted away from the start drop on this edge
        private bool _detached;
        private readonly List<DrawEdge> _edgeBuffer = new();

        // Where the rail wants the player to go, refreshed each FixedUpdate; zero = nothing to
        // point at. Cached rather than recomputed on demand so the overhead arrow reading it every
        // frame doesn't re-query the network (and doesn't stomp _edgeBuffer mid-selection).
        private Vector3 _guidance;
        private Color _guidanceColor = Color.white;

        /// <summary>
        /// The direction the rail wants the player to travel: along the edge being painted (toward
        /// its far anchor), or — while anchored and undecided — out along the edge that still needs
        /// paint. False when off the rail or nothing is left to paint here.
        ///
        /// Note this points at the TARGET even when the player is currently sliding backwards, so
        /// it stays a hint about where to go rather than a mirror of which way they happen to face.
        ///
        /// <paramref name="color"/> is that edge's authored colour, so a hint can name the edge it
        /// means the same way the level already does — by colour, not just by direction.
        /// </summary>
        public bool TryGetGuidance(out Vector3 heading, out Color color)
        {
            heading = _guidance;
            color = _guidanceColor;
            return _guidance.sqrMagnitude > 0.0001f;
        }

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
            _farthestAlong = 0f;
            _guidance = Vector3.zero;
            if (movement != null) movement.MovementLocked = false;
        }

        // Fill the rest of the edge being painted, then leave the rail.
        private void FinishEdge()
        {
            _edge.PaintFrom(_currentPart, _currentPart == _edge.A ? 1f : 0f);
            DetachAndChain();
        }

        // An edge now finishes ON its far drop, and the next edge's drop sits 0.2–0.45 away from
        // it — so the player is already standing inside it and will never get a fresh touch.
        // Before, walking into that drop was what started the next edge; keep that feel by
        // treating "finished while standing in an open drop" as touching it (anchored, arrow
        // shows the way). Escapes (enemy hit, backing off the start drop) use plain Detach.
        private void DetachAndChain()
        {
            Detach();
            if (interact != null && interact.TryGetOverlappingPart(HasOpenEdge, out DrawPart next))
                OnPartTouched(next);
        }

        private bool HasOpenEdge(DrawPart part)
        {
            EdgeNetwork net = ResolveNetwork(part);
            if (net == null) return false;
            net.GetEdgesTouching(part, _edgeBuffer);
            for (int i = 0; i < _edgeBuffer.Count; i++)
                if (!_edgeBuffer[i].IsComplete && _edgeBuffer[i].Other(part) != null) return true;
            return false;
        }

        // Touches are enter-only, but at a corner the next edge's drop sits ~0.4 from the one just
        // reached, so the player is usually already standing inside it when an edge completes — no
        // fresh touch ever fires and they push and push without the rail catching (Kaan: Level 13,
        // the plus). While free, pushing along an open edge of a drop they're standing in counts as
        // touching it. Movement stays free otherwise, so nothing locks them at the corner.
        private Vector3 _intentInput;
        private bool TryAttachFromOverlap()
        {
            _intentInput = ResolveWorldInput();
            if (_intentInput.sqrMagnitude < 0.0001f || interact == null) return false;
            if (!interact.TryGetOverlappingPart(HasOpenEdgeAlongIntent, out DrawPart part)) return false;
            OnPartTouched(part);
            return true;
        }

        private bool HasOpenEdgeAlongIntent(DrawPart part)
        {
            EdgeNetwork net = ResolveNetwork(part);
            if (net == null) return false;
            net.GetEdgesTouching(part, _edgeBuffer);
            for (int i = 0; i < _edgeBuffer.Count; i++)
            {
                DrawEdge e = _edgeBuffer[i];
                if (e.IsComplete || e.Other(part) == null) continue;
                Vector3 dir = part == e.A ? e.TangentAt(0f) : -e.TangentAt(1f);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;
                if (Vector3.Dot(_intentInput, dir.normalized) > selectThreshold) return true;
            }
            return false;
        }

        private void OnPartTouched(DrawPart part)
        {
            // Touching the far drop's trigger does NOT finish the edge any more: trigger contact
            // happens ~0.45 short of the drop, which left the player parked beside the corner and
            // off-line with the next edge, so walking on missed that edge's drop entirely (Kaan:
            // Level 13, the plus). The arrival check in FixedUpdate finishes it on the drop itself.
            // We also never re-anchor onto the target here — that re-locked the player on a new rail.
            if (_edge != null && part == _targetPart) return;

            // Every edge owns its own drops, so a corner holds two of them a drop-gap apart:
            // painting toward one, the player clips the NEIGHBOUR edge's trigger first. Falling
            // through to the re-anchor below would silently discard an almost-finished edge —
            // it freezes at ~95%, AllCompleted never fires, and the player has to walk back and
            // repaint it. Past the halfway mark the edge is committed, so ignore foreign drops
            // and let the arrival check in FixedUpdate finish it properly. Near the start,
            // re-picking a different edge from the same corner is still legitimate.
            if (_edge != null && _localT >= 0.5f) return;

            // Already on the rail (anchored or sliding), a neighbouring drop is only a reason to
            // switch if the player is actually pushing along ITS open edge. At a concave corner the
            // two drops sit ~0.4 apart, so just leaving one clips the other: unconditionally
            // re-anchoring there locked the player onto an edge pointing across their input, and
            // they ping-ponged between the two drops unable to draw (Kaan: Level 13, the plus).
            if (_currentPart != null && !_detached && part != _currentPart)
            {
                _intentInput = ResolveWorldInput();
                if (!HasOpenEdgeAlongIntent(part)) return;
            }

            // Fresh attach, or re-pick from the same corner: lock to this anchor and wait for
            // a stick direction to choose an edge.
            _currentPart = part;
            _targetPart = null;
            _edge = null;
            _localT = 0f;
            _farthestAlong = 0f;
            _detached = false;
            if (_network == null || !_network.isActiveAndEnabled) _network = ResolveNetwork(part);
        }

        private void FixedUpdate()
        {
            if (interact == null) return;

            // Knockback owns movement for its brief duration — don't touch MovementLocked
            // or try to drive the rail until it's done. Drop the hint too: being flung away from
            // an enemy is not the moment to be told to walk back into it.
            if (knockback != null && knockback.IsActive) { _guidance = Vector3.zero; return; }

            // Not painting: no anchor visited yet, or detached to flee.
            if (_currentPart == null || _detached)
            {
                _guidance = Vector3.zero;
                if (movement != null) movement.MovementLocked = false;
                if (!TryAttachFromOverlap()) return;
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
                    FinishEdge();
                    return;
                }
            }

            // Anchored with no edge chosen is exactly when the player is asking "where now?" — the
            // rail has them locked in place with nothing on screen saying which way it wants them
            // to go. Turn to face the edge that still needs paint; RailDirectionArrow puts the same
            // heading over their head. Must run BEFORE the released-input gate below, since standing
            // still with the stick untouched is the case this exists for.
            if (_edge == null)
            {
                _guidanceColor = ColorOf(ResolveNextEdge(out _guidance));
                if (_guidance.sqrMagnitude > 0.0001f)
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation, Quaternion.LookRotation(_guidance, Vector3.up),
                        aimTurnSpeed * Time.deltaTime);
            }

            if (released) return;   // no input → hold position

            // Anchored but pushing away from this drop's edge: if they're also standing in a
            // neighbouring drop whose open edge matches the push, hand over to it (the enter for
            // that drop already fired and was ignored above).
            if (_edge == null && !TrySelectEdge(worldInput)
                && !(TryAttachFromOverlap() && TrySelectEdge(worldInput))) return;

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
            // Same analog feel as free movement: a gentle push paints slowly and precisely.
            if (GameServices.Config != null) speed *= GameServices.Config.SpeedFactor(movement.MoveInput.magnitude);
            _localT = Mathf.Clamp01(_localT + along * speed * Time.deltaTime / len);   // arc-length normalized
            _farthestAlong = Mathf.Max(_farthestAlong, _localT * len);

            // Backed all the way onto the start drop after really leaving it → step off the rail
            // (Kaan: mid-edge, an enemy closes in, you retreat to the drop and run). The paint is
            // kept. Only armed once the player got escapeArmDistance away, so hovering inside the
            // drop can't toggle on/off — there it still just re-picks an edge as before.
            if (along < 0f && _localT <= 0f && _farthestAlong >= escapeArmDistance)
            {
                Detach();
                return;
            }

            float edgeT = _currentPart == _edge.A ? _localT : 1f - _localT;
            _edge.PaintFrom(_currentPart, edgeT);

            // Edge done — the two painted spans met in the middle (arrival at the far anchor
            // is handled above, before the input gate).
            if (_edge.IsComplete) { DetachAndChain(); return; }

            Vector3 targetPos = _edge.PointAt(edgeT);
            Vector3 delta = targetPos - transform.position;
            delta.y = 0f;
            _characterController.Move(delta);

            // Re-evaluate heading at the new point so the player visibly curves along the arc.
            // (No magnitude check needed any more — below the deadzone we already returned.)
            Vector3 headNow = _currentPart == _edge.A ? _edge.TangentAt(edgeT) : -_edge.TangentAt(edgeT);
            transform.rotation = Quaternion.LookRotation(headNow * along, Vector3.up);

            // The hint keeps pointing at the far anchor (no `along`), so backing up along the edge
            // shows the arrow ahead of the player rather than following them backwards.
            Vector3 hint = headNow; hint.y = 0f;
            _guidance = hint.sqrMagnitude > 0.0001f ? hint.normalized : Vector3.zero;
            _guidanceColor = ColorOf(_edge);
        }

        // The edge the hint refers to, and the direction out of the current anchor along it. Both
        // come from the same edge on purpose: the arrow's colour has to name the edge its direction
        // points at, or it would be pointing at one edge while wearing another's colour.
        //
        // On a closed loop the choice is normally unambiguous: you arrive at a corner having just
        // painted the edge behind you, leaving exactly one open. Only the very first anchor has two,
        // and there the player's own facing breaks the tie so the hint agrees with them instead of
        // spinning them.
        private DrawEdge ResolveNextEdge(out Vector3 heading)
        {
            heading = Vector3.zero;
            if (_currentPart == null) return null;
            if (_network == null || !_network.isActiveAndEnabled) _network = ResolveNetwork(_currentPart);
            if (_network == null) return null;

            _network.GetEdgesTouching(_currentPart, _edgeBuffer);

            DrawEdge best = null;
            float bestDot = float.NegativeInfinity;
            Vector3 facing = transform.forward;
            for (int i = 0; i < _edgeBuffer.Count; i++)
            {
                DrawEdge e = _edgeBuffer[i];
                if (e.IsComplete || e.Other(_currentPart) == null) continue;

                // Leave along the tangent AT this anchor — on an arc that differs from the straight
                // line to the far sphere, and pointing at the chord would aim off the rail.
                Vector3 dir = _currentPart == e.A ? e.TangentAt(0f) : -e.TangentAt(1f);
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;
                dir.Normalize();

                float dot = Vector3.Dot(facing, dir);
                if (dot > bestDot) { bestDot = dot; best = e; heading = dir; }
            }
            return best;
        }

        // An edge's authored colour — what its paint and wall are drawn in. White when there is no
        // edge, so a hint never inherits a stale colour from the last one.
        private Color ColorOf(DrawEdge edge)
            => edge != null && _network != null ? _network.ColorOf(edge) : Color.white;

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
            _farthestAlong = 0f;
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
