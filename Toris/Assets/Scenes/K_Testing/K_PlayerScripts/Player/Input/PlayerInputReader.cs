using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Actions (drag from .inputactions asset)")]
    [SerializeField] private InputActionReference moveAction;        // Value/Vector2
    [SerializeField] private InputActionReference shootMouseAction;  // Button (Mouse Left, etc.)

    public Vector2 Move { get; private set; }

    // Fire when shoot starts (press) / releases (button up)
    public event Action OnShootStarted;
    public event Action OnShootReleased;

    // Optional polling helper (e.g., for charge UI)
    public bool IsShootHeld =>
        shootMouseAction != null &&
        shootMouseAction.action != null &&
        shootMouseAction.action.IsPressed();

    void OnEnable()
    {
        moveAction?.action?.Enable();
        if (shootMouseAction?.action != null)
        {
            shootMouseAction.action.Enable();
            shootMouseAction.action.started += OnShootStartedCallback;
            shootMouseAction.action.canceled += OnShootReleasedCallback;
        }
    }

    void OnDisable()
    {
        if (shootMouseAction?.action != null)
        {
            shootMouseAction.action.started -= OnShootStartedCallback;
            shootMouseAction.action.canceled -= OnShootReleasedCallback;
            shootMouseAction.action.Disable();
        }

        moveAction?.action?.Disable();
    }

    void Update()
    {
        var act = moveAction?.action;
        Move = act != null ? act.ReadValue<Vector2>() : Vector2.zero;
    }

    // --- private wrappers so we can cleanly unsubscribe ---
    void OnShootStartedCallback(InputAction.CallbackContext _)
        => OnShootStarted?.Invoke();

    void OnShootReleasedCallback(InputAction.CallbackContext _)
        => OnShootReleased?.Invoke();
}
