using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [Header("Movement"), Space]
    [SerializeField] private float playerSpeed = 1.5f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    [Header("Components"), Space]
    [SerializeField] private Animator playerAnim;
    
    private PlayerControls _controls; //controls for player (New Input System)
    private Vector2 _move; // Vector2 Input for character movement
    private float _turnSmoothVelocity; // The time for smoothing character turn
    //Gravity variables
    private const float Gravity = -9.81f; // Gravity const 
    private Vector3 _velocity = Vector3.zero; // Velocity for gravity
    private Transform _camTransform; // Cam Transform for smooth turn
    private CharacterController _playerCharController; // CharacterController for movement functions
    
    
    private static readonly int BIsDancing = Animator.StringToHash("b_isDancing");

    private void OnEnable()
    {
        _controls.Player.Enable();
    }
    private void OnDisable()
    {
        _controls.Player.Disable();
    }
    private void Awake()
    {
        _controls = new PlayerControls();
        _controls.Player.Move.performed += ctx =>
        {
            _move = ctx.ReadValue<Vector2>();
        };
        _controls.Player.Move.canceled += ctx => 
        {
            _move = Vector2.zero;
        };
        if(_playerCharController == null)
        {
            _playerCharController = GetComponent<CharacterController>();
        }
        if(_camTransform == null)
        {
            _camTransform = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>();
        }

        if (playerAnim == null)
        {
            playerAnim = GetComponentInChildren<Animator>();
        }
    }
    private void FixedUpdate()
    {
        if (!GameManager.isGameWon)
        {
            Move();
            AddGravity();
        }
        else
        {
            playerAnim.SetBool(BIsDancing,true);
        }
    }
    private void Move()
    {
        
        Vector3 direction = new Vector3(_move.x, 0f, _move.y).normalized;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _camTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            _playerCharController.Move(moveDirection.normalized * (playerSpeed * Time.deltaTime));  
        }
    }
    private void AddGravity()
    {
        _velocity.y += Gravity * Time.deltaTime;
        _playerCharController.Move(_velocity * Time.deltaTime);
    }
}
