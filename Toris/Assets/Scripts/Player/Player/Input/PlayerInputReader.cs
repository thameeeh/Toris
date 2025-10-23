using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Actions (drag from .inputactions asset)")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _shootMouseAction;
    [SerializeField] private InputActionReference _dashAction;

    public Vector2 Move { get; private set; }

    public event Action OnShootStarted;
    public event Action OnShootReleased;
    public event Action OnDashPressed;

    public bool IsShootHeld =>
        _shootMouseAction != null &&
        _shootMouseAction.action != null &&
        _shootMouseAction.action.IsPressed();

    void OnEnable()
    {
        if (_shootMouseAction?.action != null)
        {
            _shootMouseAction.action.Enable();
            _shootMouseAction.action.started += OnShootStartedCallback;
            _shootMouseAction.action.canceled += OnShootReleasedCallback;
        }

        if (_dashAction?.action != null)
        {
            _dashAction?.action.Enable();
            _dashAction.action.performed += OnDashPerformed;
        }

        _moveAction?.action?.Enable();
    }

    void OnDisable()
    {
        if (_shootMouseAction?.action != null)
        {
            _shootMouseAction.action.started -= OnShootStartedCallback;
            _shootMouseAction.action.canceled -= OnShootReleasedCallback;
            _shootMouseAction.action.Disable();
        }

        if (_dashAction?.action != null)
        {
            _dashAction?.action.Disable();
            _dashAction.action.performed -= OnDashPerformed;
        }

        _moveAction?.action?.Disable();
    }

    void Update()
    {
        var act = _moveAction?.action;
        Move = act != null ? act.ReadValue<Vector2>() : Vector2.zero;
    }


    // private wrappers so we can cleanly unsubscribe
    void OnShootStartedCallback(InputAction.CallbackContext _)
        => OnShootStarted?.Invoke();

    void OnShootReleasedCallback(InputAction.CallbackContext _)
        => OnShootReleased?.Invoke();
    void OnDashPerformed(InputAction.CallbackContext _) 
        => OnDashPressed?.Invoke();
}
