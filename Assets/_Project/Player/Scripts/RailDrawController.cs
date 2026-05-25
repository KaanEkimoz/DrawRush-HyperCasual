using UnityEngine;
using Studios208.DrawRush.Core;
using Studios208.DrawRush.Drawing;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// Constrains player movement to polygon edges while drawing. Free movement
    /// (<see cref="ThirdPersonMovement"/>) is locked when <see cref="PlayerInteract"/>
    /// reports a chain has started; the player then slides only along an edge from the
    /// current anchor to a chosen neighbor. Edge selection follows the input direction
    /// (nearest neighbor edge). Reaching the far corner is handled by PlayerInteract's
    /// existing collision-based, neighbor-gated chain — this controller only shapes the
    /// motion, it does not advance the chain itself. When the loop closes, free movement
    /// is restored.
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
        [Tooltip("How close (m) to the current corner counts as 'slid back', cancelling the edge.")]
        [SerializeField] private float cancelRadius = 0.2f;

        private CharacterController _characterController;
        private bool _railActive;
        private Transform _currentAnchor;
        private DrawPart _currentPart;
        private DrawPart _targetPart;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            if (interact == null) interact = GetComponent<PlayerInteract>();
            if (movement == null) movement = GetComponent<ThirdPersonMovement>();
        }

        private void OnEnable()
        {
            if (interact == null) return;
            interact.ChainStarted += OnChainStarted;
            interact.ChainEnded += OnChainEnded;
        }

        private void OnDisable()
        {
            if (interact == null) return;
            interact.ChainStarted -= OnChainStarted;
            interact.ChainEnded -= OnChainEnded;
        }

        private void OnChainStarted()
        {
            _railActive = true;
            _targetPart = null;
            if (movement != null) movement.MovementLocked = true;
        }

        private void OnChainEnded()
        {
            _railActive = false;
            _currentAnchor = null;
            _currentPart = null;
            _targetPart = null;
            if (movement != null) movement.MovementLocked = false;
        }

        private void FixedUpdate()
        {
            if (!_railActive || interact == null) return;

            // The chain's current anchor is owned by PlayerInteract. When it changes we
            // just reached a new corner (or the very first one) — reset edge selection.
            Transform anchor = interact.CurrentAnchor;
            if (anchor == null) return;
            if (anchor != _currentAnchor)
            {
                _currentAnchor = anchor;
                _currentPart = anchor.GetComponent<DrawPart>();
                _targetPart = null;
            }
            if (_currentPart == null) return;

            Vector3 worldInput = ResolveWorldInput();
            if (worldInput.sqrMagnitude < 0.0001f) return;

            if (_targetPart == null)
            {
                _targetPart = PickEdge(_currentPart, worldInput);
                if (_targetPart == null) return;
            }

            Vector3 edgeDir = _targetPart.transform.position - _currentAnchor.position;
            edgeDir.y = 0f;
            if (edgeDir.sqrMagnitude < 0.0001f) return;
            edgeDir.Normalize();

            // Slide along the edge by the input's component along it (forward / back).
            float along = Vector3.Dot(worldInput, edgeDir);
            float speed = railSpeed > 0f
                ? railSpeed
                : (GameServices.Config != null ? GameServices.Config.playerSpeed : 2.7f);
            _characterController.Move(edgeDir * (along * speed * Time.deltaTime));

            // Face the slide direction.
            Vector3 face = edgeDir * Mathf.Sign(along);
            if (face.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(face, Vector3.up);
            }

            // Slid back onto the current corner → release the edge so another can be picked.
            Vector3 toCurrent = _currentAnchor.position - transform.position;
            toCurrent.y = 0f;
            if (along < 0f && toCurrent.sqrMagnitude < cancelRadius * cancelRadius)
            {
                _targetPart = null;
            }
        }

        /// <summary>Camera-relative world direction of the movement input (matches
        /// ThirdPersonMovement's mapping), or zero if below the deadzone.</summary>
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

        /// <summary>Picks the neighbor whose edge direction best aligns with the input,
        /// above <see cref="selectThreshold"/>. Completed neighbors are skipped.</summary>
        private DrawPart PickEdge(DrawPart from, Vector3 worldInput)
        {
            var neighbors = from.Neighbors;
            if (neighbors == null) return null;

            DrawPart best = null;
            float bestDot = selectThreshold;
            for (int i = 0; i < neighbors.Count; i++)
            {
                DrawPart n = neighbors[i];
                // Don't skip completed neighbors: the chain completes each anchor as it
                // advances, and the closing edge returns to the (now completed) first
                // anchor. PlayerInteract decides what each touch means — rail only moves.
                if (n == null) continue;

                Vector3 dir = n.transform.position - from.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f) continue;

                float dot = Vector3.Dot(worldInput, dir.normalized);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = n;
                }
            }
            return best;
        }
    }
}
