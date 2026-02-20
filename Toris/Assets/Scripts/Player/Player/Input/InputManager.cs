using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, InputSystem_Actions.IPlayerActions, InputSystem_Actions.IUIActions
{

    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private ItemPickEventSO _itemPicker;

    private InputSystem_Actions _inputActions;

    private void OnEnable()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
        _inputActions.Player.SetCallbacks(this);
        _inputActions.UI.SetCallbacks(this);
    }

    private void OnDisable()
    {
        _inputActions.Player.SetCallbacks(null);
        _inputActions.UI.SetCallbacks(null);
        _inputActions.Disable();
    }

    private void OnValidate()
    {
        if (_inputReader == null)
        {
            Debug.LogError($"<b>[InputReaderSO]</b> is missing on GameObject: <b>{name}<b>", this);
        }
        if (_itemPicker == null)
        {
            Debug.LogError($"<b>[ItemPickEventSO]</b> is missing on GameObject: <b>{name}<b>", this);
        }
    }

    // -------- IPlayerActions implementation --------
    public void OnJump(InputAction.CallbackContext context) {}
    public void OnLook(InputAction.CallbackContext context) {}
    public void OnMove(InputAction.CallbackContext context) 
    {
        _inputReader.SetMove(context.ReadValue<Vector2>()); 
    }
    public void OnSprint(InputAction.CallbackContext context) 
    {
        if (context.performed)
        {
            _inputReader.OnDashPressed?.Invoke();
        }
    }

    public void OnAbility1(InputAction.CallbackContext context) 
    {
        if (context.started)
        {
            _inputReader.OnAbility1Pressed?.Invoke();
        }
    }
    public void OnAbility2(InputAction.CallbackContext context) 
    {
        if(context.started)
        {
            _inputReader.isAbility2Held = true;
            _inputReader.OnAbility2Started?.Invoke();
        }
        else if(context.canceled)
        {
            _inputReader.isAbility2Held = false;
            _inputReader.OnAbility2Released?.Invoke();
        }
    }
    public void OnAttack(InputAction.CallbackContext context) 
    {
        if (context.started)
        {
            _inputReader.IsShootHeld = true;
            _inputReader.OnShootStarted?.Invoke();
        }
        else if (context.canceled)
        {
            _inputReader.IsShootHeld = false;
            _inputReader.OnShootReleased?.Invoke();
        }
    }
    public void OnCrouch(InputAction.CallbackContext context) { /* Handle Crouch */ }
    public void OnInteract(InputAction.CallbackContext context) 
    {
        _inputReader.OnInteractPressed?.Invoke();
        _itemPicker.OnItemPick?.Invoke();
    }
    public void OnNext(InputAction.CallbackContext context) { /* Handle Next */ }
    public void OnPause(InputAction.CallbackContext context) {}
    public void OnPrevious(InputAction.CallbackContext context) { /* Handle Previous */ }


    // -------- IUIActions implementation --------
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context) { }
    public void OnPoint(InputAction.CallbackContext context) { }
    public void OnClick(InputAction.CallbackContext context) { }
    public void OnRightClick(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }
}
