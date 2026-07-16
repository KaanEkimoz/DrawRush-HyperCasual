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

        [Tooltip("Animator layer index that locks the arms while idling (Avatar Mask = upper " +
                 "body). Weight is driven to 1 only while the Base Layer is in the Idle state.")]
        [SerializeField] private int idleArmsLayer = 1;
        [SerializeField] private float idleArmsBlend = 10f;

        // Minimum raw stick deflection that counts as input. Direction is normalised afterwards,
        // so anything past this moves at the SAME speed — the stick steers, it never throttles.
        private const float InputDeadzone = 0.1f;

        private PlayerControls _controls;
        private Vector2 _move;
        private float _turnSmoothVelocity;
        private Vector3 _velocity = Vector3.zero;
        private CharacterController _characterController;
        private GameState _state;
        private bool _hasWon;
        private int _idleStateHash;

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
            if (knockback != null && knockback.IsActive) { ApplyGravity(); return; }
            if (!MovementLocked) Move();
            ApplyGravity();
            // Drive the Run ↔ Idle blend from input magnitude. RailPaintController locks
            // movement while sliding, but the player is still actively moving on the rail —
            // report the rail's input so the run anim stays active.
            if (playerAnim != null) playerAnim.SetFloat(AnimatorIds.Speed, _move.magnitude);
        }

        private void Move()
        {
            // Dead-zone the RAW stick, then normalise. The check used to run on the already
            // normalised vector, whose magnitude is only ever 1 or 0 — so it gated nothing and
            // the faintest touch or stick drift launched the player at full speed.
            if (_move.sqrMagnitude < InputDeadzone * InputDeadzone) return;
            var direction = new Vector3(_move.x, 0f, _move.y).normalized;

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
