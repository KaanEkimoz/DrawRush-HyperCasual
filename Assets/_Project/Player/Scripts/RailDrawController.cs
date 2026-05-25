using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Per-edge rail movement while drawing. The player walks freely until touching a
    /// DrawPoint. From an anchor, pushing the stick toward a neighbor slides the player
    /// straight along that one edge (free movement is locked during the slide). On
    /// reaching the far corner the edge is done: the player STOPS and movement is
    /// released back to free — it does not auto-continue. A fresh push (after releasing
    /// the stick) starts the next edge from the new corner. Pushing away from any edge
    /// leaves the player free. When the loop closes, CurrentAnchor clears and the player
    /// is free again — preserving enemy evasion outside of drawing.
    /// </summary>
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
        // After arriving at a corner, require the stick to be released before a new edge
        // can start — so the player stops instead of auto-continuing to the next edge.
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

            // Not drawing (no anchor visited yet, or the loop just closed) → free movement.
            if (anchor == null)
            {
                Unlock();
                _currentAnchor = null; _currentPart = null; _targetPart = null;
                return;
            }

            // Reached a new corner (or first touch): edge complete → stop + free, and wait
            // for a fresh stick push before the next edge can begin.
            if (anchor != _currentAnchor)
            {
                _currentAnchor = anchor;
                _currentPart = anchor.GetComponent<DrawPart>();
                _targetPart = null;
                _needRelease = true;
                SnapToAnchor(anchor);   // centre the player so every edge starts symmetric
                Unlock();
                return;
            }
            if (_currentPart == null) { Unlock(); return; }

            Vector3 worldInput = ResolveWorldInput();
            if (worldInput.sqrMagnitude < 0.0001f)
            {
                _needRelease = false;   // stick released → a new push may start an edge
                _targetPart = null;
                Unlock();
                return;
            }
            if (_needRelease) { Unlock(); return; }   // still holding the push from arrival

            if (_targetPart == null)
            {
                _targetPart = PickEdge(_currentPart, worldInput);
                if (_targetPart == null) { Unlock(); return; }   // not toward an edge → free
            }

            Vector3 edgeDir = _targetPart.transform.position - _currentAnchor.position;
            edgeDir.y = 0f;
            if (edgeDir.sqrMagnitude < 0.0001f) { Unlock(); return; }
            edgeDir.Normalize();

            float along = Vector3.Dot(worldInput, edgeDir);
            if (along <= 0f) { _targetPart = null; Unlock(); return; }   // pushing away → free

            if (movement != null) movement.MovementLocked = true;
            float speed = railSpeed > 0f
                ? railSpeed
                : (GameServices.Config != null ? GameServices.Config.playerSpeed : 2.7f);
            _characterController.Move(edgeDir * (along * speed * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(edgeDir, Vector3.up);
        }

        private void Unlock()
        {
            if (movement != null) movement.MovementLocked = false;
        }

        // Centre the player on the anchor (x/z) so the next edge starts from the corner,
        // not from an off-centre touch point — otherwise sliding feels biased to one side.
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
                if (n == null) continue;

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
