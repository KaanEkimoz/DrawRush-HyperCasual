using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Constrains the player to a single polygon edge while drawing. Two spheres define an
    /// edge; touching either one starts the rail. The player then slides ONLY along the
    /// straight line between the current anchor and the chosen neighbor, freely back and
    /// forth (parameterised by t in [0,1]) — never off the line. Pushing the stick toward
    /// a neighbor selects that edge; pushing along it moves forward (t→1) or back (t→0).
    /// Reaching the far end (t≈1) lets PlayerInteract's collision-based chain spawn the
    /// line and advance to that anchor, from which the next edge can be chosen. Returning
    /// to the start (t≈0) frees the edge so a different neighbor can be picked. While a
    /// chain is active the player is fully locked to the rail; free movement returns only
    /// when the loop closes.
    ///
    /// Runs before ThirdPersonMovement so the movement lock applies in the same physics
    /// step (no one-frame leak of free movement).
    /// </summary>
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class RailDrawController : MonoBehaviour
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
        private Transform _currentAnchor;
        private DrawPart _currentPart;
        private DrawPart _targetPart;
        private float _edgeT;   // 0 = at current anchor, 1 = at target anchor

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (interact == null) interact = GetComponent<PlayerInteract>();
            if (movement == null) movement = GetComponent<ThirdPersonMovement>();
        }

        private void FixedUpdate()
        {
            if (interact == null) return;

            Transform anchor = interact.CurrentAnchor;

            // Not drawing (no anchor visited yet, or loop just closed) → free movement.
            if (anchor == null)
            {
                if (movement != null) movement.MovementLocked = false;
                _currentAnchor = null; _currentPart = null; _targetPart = null;
                return;
            }

            // Drawing → player is fully rail-locked (no free / off-edge movement).
            if (movement != null) movement.MovementLocked = true;

            // New corner (or first touch): reset to the start of a fresh edge selection.
            if (anchor != _currentAnchor)
            {
                _currentAnchor = anchor;
                _currentPart = anchor.GetComponent<DrawPart>();
                _targetPart = null;
                _edgeT = 0f;
            }
            if (_currentPart == null) return;

            Vector3 worldInput = ResolveWorldInput();
            if (worldInput.sqrMagnitude < 0.0001f) return;   // no input → hold position (locked)

            // No edge yet → pick the neighbor best aligned with the push.
            if (_targetPart == null)
            {
                _targetPart = PickEdge(_currentPart, worldInput);
                if (_targetPart == null) return;
                _edgeT = 0f;
            }

            Vector3 rawEdge = _targetPart.transform.position - _currentAnchor.position;
            rawEdge.y = 0f;
            float len = rawEdge.magnitude;
            if (len < 0.001f) { _targetPart = null; return; }
            Vector3 edgeDir = rawEdge / len;

            // Forward (toward target) when pushing along the edge, backward when pushing
            // against it — free movement along the line, clamped to the two spheres.
            float along = Vector3.Dot(worldInput, edgeDir);
            float speed = railSpeed > 0f
                ? railSpeed
                : (GameServices.Config != null ? GameServices.Config.playerSpeed : 2.7f);
            _edgeT = Mathf.Clamp01(_edgeT + along * speed * Time.deltaTime / len);

            Vector3 targetPos = _currentAnchor.position + rawEdge * _edgeT;
            Vector3 delta = targetPos - transform.position;
            delta.y = 0f;
            _characterController.Move(delta);

            if (Mathf.Abs(along) > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(edgeDir * Mathf.Sign(along), Vector3.up);
            }

            // Slid all the way back to the start anchor → release so another edge can be chosen.
            if (_edgeT <= 0.001f && along < 0f) _targetPart = null;
        }

        private Vector3 ResolveWorldInput()
        {
            Vector2 input = movement != null ? movement.MoveInput : Vector2.zero;
            Vector3 raw = new Vector3(input.x, 0f, input.y);
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

                Vector3 dir = n.transform.position - from.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;

                float dot = Vector3.Dot(worldInput, dir.normalized);
                if (dot > bestDot) { bestDot = dot; best = n; }
            }
            return best;
        }
    }
}
