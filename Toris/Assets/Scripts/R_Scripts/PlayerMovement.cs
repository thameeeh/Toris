using System;
using System.Data;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;


public class PlayerMovement : MonoBehaviour
{
    public State idleState;
    public State walkState;
    public State runState;

    State _state;

    public float speed { get; set; }

    public bool isRunning { get; private set; }

    private InputSystem_Actions _playerInputActions;
    public Vector2 _input { get; private set; } 
    private Rigidbody2D _rigidbody2D;
    [SerializeField]
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Disable();
    }

    private void Awake()
    {
        _playerInputActions = new InputSystem_Actions();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        if (_animator == null)
            _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        idleState.Setup(_animator, this);
        walkState.Setup(_animator, this);
        runState.Setup(_animator, this);
        _state = idleState;
    }

    private void FixedUpdate()
    {
        Move();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        _spriteRenderer.flipX = _input.x < 0;
    }

    private void Update()
    {
        GatherInput();
        if (_state.isComplete)
        {
            SelectState();
        }
        _state.Do();
    }

    private void Move()
    {
        Vector2 moveDirection = _input;
        moveDirection = ConvertIntoIsometric(moveDirection);
        
        //Debug.Log(moveDirection);

        moveDirection = moveDirection * speed * Time.deltaTime;

        _rigidbody2D.MovePosition(_rigidbody2D.position + moveDirection);
    }

    private Vector2 ConvertIntoIsometric(Vector2 v2) 
    {
        /*
         * multiply Moving Vector by [   1,   1]
         *                           [-0.5, 0.5] 
         * to convert it into isometric space, basicaly we are squishing the y axis by half
        */

        Vector2 result = new(v2.x, v2.y / 2); 
        return result.normalized;
    }

    private void GatherInput() 
    {
        _input = _playerInputActions.Player.Move.ReadValue<Vector2>();
        isRunning = _playerInputActions.Player.Sprint.IsPressed();
    }

    private void SelectState() 
    {
        float magnitude = _input.magnitude;

        if (magnitude <= 0)
        {
            _state = idleState;
            isRunning = false;
        }
        else if (!isRunning && magnitude > 0)
        {
            _state = walkState;
        }
        else _state = runState;

        _state.Enter();
    }
}
