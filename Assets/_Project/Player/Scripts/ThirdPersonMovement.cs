using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// CharacterController-based third-person movement. Camera-relative direction
    /// is computed from <see cref="GameServices.MainCamera"/> — no GameObject.Find calls.
    /// Speed / turn / gravity come from <see cref="GameConfig"/> so they tune without
    /// recompile. Switches to the dance pose by subscribing to GameState.GameWonChanged.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMovement : MonoBehaviour
    {
        [Header("Components"), Space]
        [SerializeField] private Animator playerAnim;
        [SerializeField] private PlayerKnockback knockback;

        [Header("Movement Overrides"), Space]
        [Tooltip("If true, ignore GameConfig and use the per-instance values below.")]
        [SerializeField] private bool useLocalValues;
        [SerializeField] private float localPlayerSpeed = 1.5f;
        [SerializeField] private float localTurnSmoothTime = 0.1f;

        private PlayerControls _controls;
        private Vector2 _move;
        private float _turnSmoothVelocity;
        private Vector3 _velocity = Vector3.zero;
        private CharacterController _characterController;
        private GameState _state;
        private bool _hasWon;

        /// <summary>Latest movement input vector. Exposed so RailPaintController can reuse
        /// the same input source while it drives edge-constrained movement.</summary>
        public Vector2 MoveInput => _move;

        /// <summary>When true, free movement is suspended (rail-drawing owns the player).
        /// Gravity still applies so the character stays grounded.</summary>
        public bool MovementLocked { get; set; }

        private void Awake()
        {
            _controls = new PlayerControls();
            _controls.Player.Move.performed += ctx => _move = ctx.ReadValue<Vector2>();
            _controls.Player.Move.canceled += _ => _move = Vector2.zero;

            _characterController = GetComponent<CharacterController>();

            if (playerAnim == null)
            {
                playerAnim = GetComponentInChildren<Animator>();
            }
            if (knockback == null) knockback = GetComponent<PlayerKnockback>();
        }

        private void OnEnable()
        {
            _controls.Player.Enable();
            _state = GameServices.State;
            if (_state != null) _state.GameWonChanged += OnGameWonChanged;
        }

        private void OnDisable()
        {
            _controls.Player.Disable();
            if (_state != null) _state.GameWonChanged -= OnGameWonChanged;
        }

        private void FixedUpdate()
        {
            if (_hasWon) return;
            // Yield to PlayerKnockback while it's pushing the player away from an enemy;
            // gravity still applies so the character stays grounded.
            if (knockback != null && knockback.IsActive) { ApplyGravity(); return; }
            if (!MovementLocked) Move();
            ApplyGravity();
        }

        private void Move()
        {
            var direction = new Vector3(_move.x, 0f, _move.y).normalized;
            if (direction.magnitude < 0.1f) return;

            var camera = GameServices.MainCamera;
            float cameraY = camera != null ? camera.eulerAngles.y : 0f;
            float speed = ResolveSpeed();
            float turn = ResolveTurnSmoothTime();

            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraY;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turn);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            var moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _characterController.Move(moveDirection.normalized * (speed * Time.deltaTime));
        }

        private void ApplyGravity()
        {
            float gravity = GameServices.Config != null ? GameServices.Config.gravity : -9.81f;
            _velocity.y += gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);
        }

        private void OnGameWonChanged(bool won)
        {
            // Mirror the win state both ways: dance + freeze on win, and clear them on a
            // reset (Restart / next level), since the player is a shared object that never
            // reloads.
            _hasWon = won;
            if (playerAnim != null) playerAnim.SetBool(AnimatorIds.IsDancing, won);
        }

        private float ResolveSpeed()
            => useLocalValues || GameServices.Config == null ? localPlayerSpeed : GameServices.Config.playerSpeed;

        private float ResolveTurnSmoothTime()
            => useLocalValues || GameServices.Config == null ? localTurnSmoothTime : GameServices.Config.turnSmoothTime;
    }
}
