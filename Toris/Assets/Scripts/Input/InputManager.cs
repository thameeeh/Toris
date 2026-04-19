using UnityEngine;
using UnityEngine.InputSystem;
using OutlandHaven.UIToolkit; // for screen types
using OutlandHaven.Inventory;

public class InputManager : MonoBehaviour, InputSystem_Actions.IPlayerActions, InputSystem_Actions.IUIActions
{

    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private ItemPickEventSO _itemPicker;

    [Header("UI Events")]
    [SerializeField] private UIEventsSO _uiEvents;

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
            Debug.LogError($"<b><color=green>[ItemPickEventSO]</color></b> is missing on GameObject: <b>{name}<b>", this);
        }
        if (_uiEvents == null)
        {
            Debug.LogError($"<b><color=red>[UIEventsSO]</color></b> is missing on GameObject: <b>{name}<b>", this);
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
            _inputReader.RaiseAbilitySlotStarted(0);
        }
        else if (context.canceled)
        {
            _inputReader.RaiseAbilitySlotReleased(0);
        }
    }
    public void OnAbility2(InputAction.CallbackContext context) 
    {
        if(context.started)
        {
            _inputReader.isAbility2Held = true;
            _inputReader.OnAbility2Started?.Invoke();
            _inputReader.RaiseAbilitySlotStarted(1);
        }
        else if(context.canceled)
        {
            _inputReader.isAbility2Held = false;
            _inputReader.OnAbility2Released?.Invoke();
            _inputReader.RaiseAbilitySlotReleased(1);
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
    public void OnInteract(InputAction.CallbackContext context) // Key 'E'
    {
        if (context.started)
        {
            _inputReader.OnInteractPressed?.Invoke();
        }
        if (context.started)
            _itemPicker.OnItemPick?.Invoke();
    }
    public void OnNext(InputAction.CallbackContext context) { /* Handle Next */ }
    public void OnPause(InputAction.CallbackContext context) {}
    public void OnPrevious(InputAction.CallbackContext context) { /* Handle Previous */ }

    public void OnAbility3(InputAction.CallbackContext context)
    {
        HandleAbilitySlotInput(2, context);
    }

    public void OnAbility4(InputAction.CallbackContext context)
    {
        HandleAbilitySlotInput(3, context);
    }

    public void OnAbility5(InputAction.CallbackContext context)
    {
        HandleAbilitySlotInput(4, context);
    }

    private void HandleAbilitySlotInput(int slotIndex, InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _inputReader.RaiseAbilitySlotStarted(slotIndex);
        }
        else if (context.canceled)
        {
            _inputReader.RaiseAbilitySlotReleased(slotIndex);
        }
    }


    // -------- IUIActions implementation --------
    public void OnNavigate(InputAction.CallbackContext context) { }
    public void OnSubmit(InputAction.CallbackContext context) { }
    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _uiEvents.OnRequestCloseAll?.Invoke();
        }
    }
    public void OnPoint(InputAction.CallbackContext context) { }
    public void OnClick(InputAction.CallbackContext context) { }
    public void OnRightClick(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.Inventory, null);
        }
    }

    public void OnToggleMage(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.Mage, null);
        }
    }

    public void OnToggleSkills(InputAction.CallbackContext context)
    {
        _uiEvents.OnRequestOpen?.Invoke(ScreenType.Skills, null);
    }
}
