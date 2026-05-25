using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Constrains the player to polygon edges while drawing. The whole time a chain is
    /// active (an anchor has been touched and the loop hasn't closed), free movement is
    /// LOCKED — the player can only slide straight along one chosen edge, never freely
    /// up/down or sideways off the rail. From an anchor, pushing toward a neighbor slides
    /// along that edge; reaching the far corner snaps to centre and stops (release the
    /// stick, then push again for the next edge). Pushing away from / not along any edge
    /// keeps the player put (still locked). When the loop closes, CurrentAnchor clears and
    /// free movement returns — preserving enemy evasion outside of drawing.
    ///
    /// Runs before ThirdPersonMovement (execution order) so MovementLocked is set for the
    /// same physics step, preventing a frame of free movement leaking through.
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
        // After arriving at a corner, require the stick to be released before the next edge
        // can start — so the player stops at each corner instead of auto-continuing.
        private bool _needRelease;

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

            // Drawing → player is rail-locked for the entire chain. No free up/down/side
            // movement; the only motion allowed is sliding along the selected edge below.
            if (movement != null) movement.MovementLocked = true;

            // Reached a new corner (or first touch): centre on it and stop; wait for a
            // fresh stick push before the next edge can begin.
            if (anchor != _currentAnchor)
            {
                _currentAnchor = anchor;
                _currentPart = anchor.GetComponent<DrawPart>();
                _targetPart = null;
                _needRelease = true;
                SnapToAnchor(anchor);
                return;
            }
            if (_currentPart == null) return;

            Vector3 worldInput = ResolveWorldInput();
            if (worldInput.sqrMagnitude < 0.0001f)
            {
                _needRelease = false;   // stick released → next push may start an edge
                _targetPart = null;
                return;
            }
            if (_needRelease) return;   // still holding the push from arrival

            if (_targetPart == null)
            {
                _targetPart = PickEdge(_currentPart, worldInput);
                if (_targetPart == null) return;   // not toward an edge → stay put (locked)
            }

            Vector3 edgeDir = _targetPart.transform.position - _currentAnchor.position;
            edgeDir.y = 0f;
            if (edgeDir.sqrMagnitude < 0.0001f) return;
            edgeDir.Normalize();

            float along = Vector3.Dot(worldInput, edgeDir);
            if (along <= 0f) { _targetPart = null; return; }   // pushing away → reselect

            float speed = railSpeed > 0f
                ? railSpeed
                : (GameServices.Config != null ? GameServices.Config.playerSpeed : 2.7f);
            // Move ONLY along the edge direction — no lateral component, so the player
            // can't drift off the rail.
            _characterController.Move(edgeDir * (along * speed * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(edgeDir, Vector3.up);
        }

        // Centre the player on the anchor (x/z) so each edge starts from the corner.
        private void SnapToAnchor(Transform anchor)
        {
            Vector3 pos = transform.position;
            pos.x = anchor.position.x;
            pos.z = anchor.position.z;
            _characterController.enabled = false;
            transform.position = pos;
            _characterController.enabled = true;
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
