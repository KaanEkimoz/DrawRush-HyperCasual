using UnityEngine;
using DrawRush.Core;

namespace DrawRush.Player
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

        [Tooltip("Animator layer index that locks the arms while idling (Avatar Mask = upper " +
                 "body). Weight is driven to 1 only while the Base Layer is in the Idle state.")]
        [SerializeField] private int idleArmsLayer = 1;
        [SerializeField] private float idleArmsBlend = 10f;

        // The on-screen stick applies its own radial deadzone and rescales past it, so this only
        // filters gamepad drift. Past it the stick only steers: speed builds up with time spent
        // moving (GameConfig.SpeedFactor), so it never has to be pushed all the way.
        private const float InputDeadzone = 0.02f;

        private PlayerControls _controls;
        private Vector2 _move;
        private float _turnSmoothVelocity;
        private Vector3 _velocity = Vector3.zero;
        private Vector3 _planarVelocity = Vector3.zero;
        private float _movingTime;
        private Vector3 _lastDirection;
        private CharacterController _characterController;
        private GameState _state;
        private bool _hasWon;
        private int _idleStateHash;

        /// <summary>Latest movement input vector. Exposed so RailPaintController can reuse
        /// the same input source while it drives edge-constrained movement.</summary>
        public Vector2 MoveInput => _move;

        /// <summary>Speed multiple from boosters (1 = none).</summary>
        public float SpeedMultiplier { get; set; } = 1f;

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
            _idleStateHash = Animator.StringToHash("Idle");
        }

        // The upper-body Avatar Mask layer should only assert itself while the player is
        // actually standing in Idle — never during Run / Hit / Dance (those need full-body
        // motion). Blend the layer weight toward 1 in Idle, 0 otherwise.
        private void Update()
        {
            if (playerAnim == null || idleArmsLayer <= 0 || idleArmsLayer >= playerAnim.layerCount) return;
            var baseState = playerAnim.GetCurrentAnimatorStateInfo(0);
            float target = baseState.shortNameHash == _idleStateHash ? 1f : 0f;
            float w = Mathf.MoveTowards(playerAnim.GetLayerWeight(idleArmsLayer), target, idleArmsBlend * Time.deltaTime);
            playerAnim.SetLayerWeight(idleArmsLayer, w);
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
            if (knockback != null && knockback.IsActive) { _planarVelocity = Vector3.zero; ApplyGravity(); return; }
            if (MovementLocked) _planarVelocity = Vector3.zero;
            else Move();
            ApplyGravity();
            // Drive the Run ↔ Idle blend. RailPaintController locks movement while sliding, but
            // the player is still actively moving on the rail, so report the stick there; in
            // free movement report the real speed so a walk animates slower than a run.
            if (playerAnim != null)
            {
                float speed01 = MovementLocked ? _move.magnitude : _planarVelocity.magnitude / Mathf.Max(0.01f, ResolveSpeed());
                playerAnim.SetFloat(AnimatorIds.Speed, speed01);
            }
        }

        private void Move()
        {
            float speed = ResolveSpeed();
            Vector3 target = Vector3.zero;
            if (_move.magnitude < InputDeadzone) { _movingTime = 0f; _lastDirection = Vector3.zero; }
            else
            {
                var camera = GameServices.MainCamera;
                float cameraY = camera != null ? camera.eulerAngles.y : 0f;
                var direction = new Vector3(_move.x, 0f, _move.y).normalized;
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraY;
                Vector3 heading = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

                // Build speed while moving; a sharp turn falls back toward the start speed. The
                // reference heading trails the stick by ~0.1 s so a quick thumb sweep still counts.
                float sharp = GameServices.Config != null ? GameServices.Config.sharpTurnAngle : 100f;
                if (_lastDirection == Vector3.zero) _lastDirection = heading;
                if (Vector3.Angle(_lastDirection, heading) > sharp) { _movingTime = 0f; _lastDirection = heading; }
                _movingTime += Time.deltaTime;
                _lastDirection = Vector3.Slerp(_lastDirection, heading, 1f - Mathf.Exp(-10f * Time.deltaTime));
                target = heading * (speed * SpeedFactor(_movingTime) * SpeedMultiplier);

                // Only the model's facing is smoothed; the velocity below follows the stick.
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, ResolveTurnSmoothTime());
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }

            // Short ramps instead of instant on/off: full speed in accelerationTime, a stop in
            // decelerationTime. Short enough to read as instant, long enough to kill the jolt.
            bool stopping = target.sqrMagnitude < 0.0001f;
            float rampTime = Mathf.Max(0.001f, stopping ? ResolveDecelerationTime() : ResolveAccelerationTime());
            _planarVelocity = Vector3.MoveTowards(_planarVelocity, target, speed / rampTime * Time.deltaTime);
            if (_planarVelocity.sqrMagnitude > 0f) _characterController.Move(_planarVelocity * Time.deltaTime);
        }

        private float SpeedFactor(float movingSeconds)
            => GameServices.Config != null ? GameServices.Config.SpeedFactor(movingSeconds) : 1f;

        private float ResolveAccelerationTime() => GameServices.Config != null ? GameServices.Config.accelerationTime : 0.08f;
        private float ResolveDecelerationTime() => GameServices.Config != null ? GameServices.Config.decelerationTime : 0.05f;

        private void ApplyGravity()
        {
            float gravity = GameServices.Config != null ? GameServices.Config.gravity : -9.81f;
            // Clamp the accumulator while grounded. Without this velocity.y grows every frame
            // for the whole session (minutes in, it passes -2000), so the player no longer
            // falls off a ledge — one step sweeps them straight through the floor. The small
            // negative bias keeps isGrounded stable instead of jittering.
            if (_characterController.isGrounded && _velocity.y < 0f) _velocity.y = -2f;
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
