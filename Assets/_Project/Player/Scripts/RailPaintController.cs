using System;
using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Edge-painting movement. Touching an anchor sphere (inside a DrawArea) attaches the
    /// player to that anchor; pushing the stick toward a neighbor selects that edge and the
    /// player slides along the straight line between the two spheres, painting the span it
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

        [Header("Tuning")]
        [Tooltip("Minimum input magnitude before an edge is picked or driven.")]
        [SerializeField] private float inputDeadzone = 0.3f;
        [Tooltip("Minimum alignment (dot) between input and an edge direction to select it.")]
        [SerializeField] private float selectThreshold = 0.4f;
        [Tooltip("Rail slide speed. When <= 0, falls back to GameConfig.playerSpeed.")]
        [SerializeField] private float railSpeed = 0f;

        private CharacterController _characterController;
        private EdgeNetwork _network;
        private DrawPart _currentPart;
        private DrawPart _targetPart;
        private DrawEdge _edge;
        private float _localT;          // 0 = at current anchor, 1 = at target anchor
        private bool _detached;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (interact == null) interact = GetComponent<PlayerInteract>();
            if (movement == null) movement = GetComponent<ThirdPersonMovement>();
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

            // Not painting: outside the draw area, no anchor visited, or detached to flee.
            if (!interact.IsInDrawArea || _currentPart == null || _detached)
            {
                if (movement != null) movement.MovementLocked = false;
                if (!interact.IsInDrawArea) { _currentPart = null; _edge = null; _detached = false; }
                return;
            }

            if (movement != null) movement.MovementLocked = true;

            Vector3 worldInput = ResolveWorldInput();
            if (worldInput.sqrMagnitude < 0.0001f) return;   // no input → hold position

            if (_edge == null && !TrySelectEdge(worldInput)) return;

            Vector3 rawEdge = _targetPart.Transform.position - _currentPart.Transform.position;
            rawEdge.y = 0f;
            float len = rawEdge.magnitude;
            if (len < 0.001f) { _edge = null; _targetPart = null; return; }
            Vector3 edgeDir = rawEdge / len;

            float along = Vector3.Dot(worldInput, edgeDir);
            float speed = railSpeed > 0f
                ? railSpeed
                : (GameServices.Config != null ? GameServices.Config.playerSpeed : 2.7f);
            _localT = Mathf.Clamp01(_localT + along * speed * Time.deltaTime / len);

            // Paint the covered span. Convert local (current→target) t to the edge's A→B t.
            float edgeT = _currentPart == _edge.A ? _localT : 1f - _localT;
            _edge.PaintFrom(_currentPart, edgeT);

            // Edge done — either the two painted spans met in the middle, or the player slid
            // the whole length to the far end. Free the player off the rail.
            if (_edge.IsComplete) { Detach(); return; }

            Vector3 targetPos = _currentPart.Transform.position + rawEdge * _localT;
            Vector3 delta = targetPos - transform.position;
            delta.y = 0f;
            _characterController.Move(delta);

            if (Mathf.Abs(along) > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(edgeDir * Mathf.Sign(along), Vector3.up);
            }
        }

        private bool TrySelectEdge(Vector3 worldInput)
        {
            DrawPart target = PickEdge(_currentPart, worldInput);
            if (target == null) return false;

            if (_network == null || !_network.isActiveAndEnabled) _network = ResolveNetwork(_currentPart);
            if (_network == null || !_network.TryGetEdge(_currentPart, target, out _edge))
            {
                _edge = null;
                return false;
            }
            _targetPart = target;
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

        private DrawPart PickEdge(DrawPart from, Vector3 worldInput)
        {
            var neighbors = from.Neighbors;
            if (neighbors == null) return null;

            DrawPart best = null;
            float bestDot = selectThreshold;
            for (int i = 0; i < neighbors.Count; i++)
            {
                DrawPart n = neighbors[i];
                if (n == null || n == from) continue;

                Vector3 dir = n.Transform.position - from.Transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;

                float dot = Vector3.Dot(worldInput, dir.normalized);
                if (dot > bestDot) { bestDot = dot; best = n; }
            }
            return best;
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
