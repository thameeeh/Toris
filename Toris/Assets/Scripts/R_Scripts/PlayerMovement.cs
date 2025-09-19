using System;
using System.Data;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Windows;


public class PlayerMovement : MonoBehaviour
{
    public State idleState;
    public State walkState;
    public State runState;

    State _state;

    public float Speed { get; private set; }            //for state logic
    public bool IsRunning { get; private set; }         //

    public Vector2 MovementVector { get; private set; } 
    private InputSystem_Actions _playerInputActions;    //input system
    private Rigidbody2D _rigidbody2D;                   //for movement
    private Animator _animator;                         //for animations
    public SpriteRenderer _spriteRenderer { get; private set; }             //only used to flip sprite on y axis -> (east || west)

    #region eneabling and disabling input system
    private void OnEnable()
    {
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Disable();
    }
    #endregion
    private void Awake()
    {
        _playerInputActions = new InputSystem_Actions();
        _rigidbody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();

        idleState.Setup(_animator, this);
        walkState.Setup(_animator, this);
        runState.Setup(_animator, this);
        _state = idleState;
    }

    private void Update()
    {
        GatherInput();

        if (_state.isComplete)
        {
            SelectState();
        }
        if(_playerInputActions.Player.Move.IsInProgress()) 
            _state.AnimationDirection(MovementVector);              //update animation direction only when movement input is pressed

        _state.Do(); //run state logic

        Move();
    }

    private void Move()
    {
        Vector2 moveDirection = MovementVector;
        moveDirection = ConvertIntoIsometric(moveDirection);
        
        Debug.Log(moveDirection);

        moveDirection = moveDirection * Speed * Time.deltaTime;

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
        MovementVector = _playerInputActions.Player.Move.ReadValue<Vector2>();
        if (MovementVector.magnitude == 0) Speed = 0;
        IsRunning = _playerInputActions.Player.Sprint.IsPressed();
    }

    private void SelectState() 
    {
        float magnitude = MovementVector.magnitude;

        if (magnitude == 0)
        {
            _state = idleState;
            Speed = 0;
        }
        else if (!IsRunning && magnitude > 0)
        {
            _state = walkState;
            Speed = 1;
        }
        else
        {
            _state = runState;
            Speed = 6;
        }
        _state.Enter();
    }
}
