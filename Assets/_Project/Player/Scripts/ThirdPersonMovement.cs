using UnityEngine;
using Studios208.DrawRush.Core;

namespace Studios208.DrawRush.Player
{
    /// <summary>
    /// CharacterController-based third-person movement. Camera-relative direction
    /// is computed from <see cref="GameServices.MainCamera"/> — no GameObject.Find calls.
    /// Speed / turn / gravity come from <see cref="GameConfig"/> so they tune without
    /// recompile.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class ThirdPersonMovement : MonoBehaviour
    {
        [Header("Components"), Space]
        [SerializeField] private Animator playerAnim;

        [Header("Movement Overrides"), Space]
        [Tooltip("If true, ignore GameConfig and use the per-instance values below.")]
        [SerializeField] private bool useLocalValues;
        [SerializeField] private float localPlayerSpeed = 1.5f;
        [SerializeField] private float localTurnSmoothTime = 0.1f;

        private static readonly int BIsDancing = Animator.StringToHash("b_isDancing");

        private PlayerControls _controls;
        private Vector2 _move;
        private float _turnSmoothVelocity;
        private Vector3 _velocity = Vector3.zero;
        private CharacterController _characterController;

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
        }

        private void OnEnable() => _controls.Player.Enable();
        private void OnDisable() => _controls.Player.Disable();

        private void FixedUpdate()
        {
            var state = GameServices.State;
            if (state != null && state.IsGameWon)
            {
                if (playerAnim != null) playerAnim.SetBool(BIsDancing, true);
                return;
            }

            Move();
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

        private float ResolveSpeed()
            => useLocalValues || GameServices.Config == null ? localPlayerSpeed : GameServices.Config.playerSpeed;

        private float ResolveTurnSmoothTime()
            => useLocalValues || GameServices.Config == null ? localTurnSmoothTime : GameServices.Config.turnSmoothTime;
    }
}
