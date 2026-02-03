using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Adapts the Unity Input System (InputSystem_Actions.inputactions)
/// into a simple API for the rest of the player code.
/// </summary>
public class PlayerInputReader : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    private InputSystem_Actions _actions;

    [Header("Debug")]
    [SerializeField] private bool _debugInput = false;
    public Vector2 Move { get; private set; }

    public event Action OnShootStarted;
    public event Action OnShootReleased;
    public event Action OnDashPressed;
    public event Action OnAbility1Pressed;
    public event Action OnInteractPressed;


    public event System.Action OnAbility2Started;
    public event System.Action OnAbility2Released;

    public bool isAbility2Held => 
        _actions != null && 
        _actions.Player.Ability2.IsPressed();

    public bool IsShootHeld =>
        _actions != null &&
        _actions.Player.Attack.IsPressed();

    private void OnEnable()
    {
        if (_actions == null)
            _actions = new InputSystem_Actions();

        _actions.Player.SetCallbacks(this);

        _actions.Enable();
    }

    private void OnDisable()
    {
        if (_actions != null)
        {
            _actions.Disable();
            _actions.Player.SetCallbacks(null);
        }
    }

    // -------- IPlayerActions implementation --------

    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
        if (_debugInput && context.performed)
            Debug.Log($"[Input] Move performed: {Move}", this);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        // Ignored for now
    }

    public void OnAttack(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (_debugInput) Debug.Log("[Input] Attack started", this);
            OnShootStarted?.Invoke();
        }
        else if (context.canceled)
        {
            if (_debugInput) Debug.Log("[Input] Attack canceled", this);
            OnShootReleased?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        if (_debugInput) Debug.Log("[Input] Interact started", this);
        OnInteractPressed?.Invoke();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        // Not used yet
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        // Not used yet
    }

    public void OnPrevious(InputAction.CallbackContext context)
    {
        // Not used yet
    }

    public void OnNext(InputAction.CallbackContext context)
    {
        // Not used yet
    }


    public void OnPause(InputAction.CallbackContext context)
    {
        // Not used yet
    }
    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (_debugInput) Debug.Log("[Input] Sprint performed (Dash)", this);
            OnDashPressed?.Invoke();
        }
    }

    public void OnAbility1(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (_debugInput) Debug.Log("[Input] Ability1 pressed", this);
            OnAbility1Pressed?.Invoke();
        }
    }

    public void OnAbility2(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            if (_debugInput) Debug.Log("[Input] Ability2 started (Rambo)", this);
            OnAbility2Started?.Invoke();
        }
        else if (context.canceled)
        {
            if (_debugInput) Debug.Log("[Input] Ability2 released (Rambo)", this);
            OnAbility2Released?.Invoke();
        }
    }
}
