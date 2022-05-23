using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonMovement : MonoBehaviour
{
    [SerializeField] private Transform camTransform;
    [SerializeField] private float playerSpeed = 1.5f;
    [SerializeField] private float turnSmoothTime = 0.1f;
    
    //InputActions for Player from New Input System
    private PlayerControls _controls;
    //Vector2 Input
    private Vector2 _move;
    private static bool isMoving;
    
    private CharacterController _playerCharController;
    private float _turnSmoothVelocity;

    //Gravity variables
    private const float Gravity = -9.81f;
    private Vector3 _velocity = Vector3.zero;
    
    //Speed
    public float Speed
    {
        get => playerSpeed;
        set => playerSpeed = value;
    }

    public static bool Moving
    {
        get => isMoving;
    }

    [SerializeField] private Animator _playerAnim;

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
        //Add Walk Animations
        _controls.Player.Move.performed += ctx =>
        {
            _move = ctx.ReadValue<Vector2>();
            isMoving = true;
        };
        //Cancel walk Animations
        _controls.Player.Move.canceled += ctx => 
        {
            _move = Vector2.zero;
            isMoving = false;
        };
    }
    void Start()
    {
        if(_playerCharController == null)
        {
           _playerCharController = GetComponent<CharacterController>();
        }
        if(camTransform == null)
        {
            camTransform = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Transform>();
        }
    }
    void FixedUpdate()
    {
        if (!GameManager.isGameWon)
        {
            Move();
            AddGravity();
        }
        else
        {
            _playerAnim.SetBool("b_isDancing",true);
        }
    }
    private void Move()
    {
        
        Vector3 direction = new Vector3(_move.x, 0f, _move.y).normalized;
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + camTransform.eulerAngles.y;
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
