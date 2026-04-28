using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using OutlandHaven.UIToolkit; // for screen types
using OutlandHaven.Inventory;

public class InputManager : MonoBehaviour, InputSystem_Actions.IPlayerActions, InputSystem_Actions.IUIActions
{
    private readonly HashSet<ScreenType> _openBlockingScreens = new();
    private readonly HashSet<string> _gameplayInputLocks = new();

    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private ItemPickEventSO _itemPicker;

    [Header("UI Events")]
    [SerializeField] private UIEventsSO _uiEvents;

    [Header("Gameplay Input Policy")]
    [Tooltip("Combat-like inputs are disabled in these scenes, but movement and interaction can still stay active.")]
    [SerializeField] private string[] _combatDisabledSceneNames = { "MainArea" };

    private InputSystem_Actions _inputActions;

    private void OnEnable()
    {
        _inputActions = new InputSystem_Actions();
        _inputActions.Enable();
        _inputActions.Player.SetCallbacks(this);
        _inputActions.UI.SetCallbacks(this);

        if (_uiEvents != null)
        {
            _uiEvents.OnScreenOpen += HandleScreenOpened;
            _uiEvents.OnScreenClose += HandleScreenClosed;
            _uiEvents.OnGameplayInputLockRequested += HandleGameplayInputLockRequested;
            _uiEvents.OnGameplayInputUnlockRequested += HandleGameplayInputUnlockRequested;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshGameplayInputState();
    }

    private void OnDisable()
    {
        if (_uiEvents != null)
        {
            _uiEvents.OnScreenOpen -= HandleScreenOpened;
            _uiEvents.OnScreenClose -= HandleScreenClosed;
            _uiEvents.OnGameplayInputLockRequested -= HandleGameplayInputLockRequested;
            _uiEvents.OnGameplayInputUnlockRequested -= HandleGameplayInputUnlockRequested;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        _openBlockingScreens.Clear();
        _gameplayInputLocks.Clear();

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
        if (_inputReader == null)
            return;

        _inputReader.SetMove(AllowsMovementInput() ? context.ReadValue<Vector2>() : Vector2.zero); 
    }
    public void OnSprint(InputAction.CallbackContext context) 
    {
        if (context.performed && AllowsDashInput())
        {
            _inputReader.OnDashPressed?.Invoke();
        }
    }

    public void OnAbility1(InputAction.CallbackContext context) 
    {
        if (!AllowsCombatInput())
            return;

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
        if (!AllowsCombatInput())
            return;

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
        if (!AllowsCombatInput())
            return;

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
        if (!AllowsInteractionInput())
            return;

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
        if (!AllowsCombatInput())
            return;

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
        if (context.performed)
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.Skills, null);
        }
    }

    public void OnToggleQuestJournal(InputAction.CallbackContext context)
    {
        if (context.performed && !HasGameplayInputBlockers())
        {
            _uiEvents.OnQuestJournalOpenRequested?.Invoke("Active");
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _openBlockingScreens.Clear();
        _gameplayInputLocks.Clear();
        RefreshGameplayInputState();
    }

    private void HandleScreenOpened(ScreenType screenType)
    {
        if (!IsGameplayBlockingScreen(screenType))
            return;

        _openBlockingScreens.Add(screenType);
        RefreshGameplayInputState();
    }

    private void HandleScreenClosed(ScreenType screenType)
    {
        if (!IsGameplayBlockingScreen(screenType))
            return;

        _openBlockingScreens.Remove(screenType);
        RefreshGameplayInputState();
    }

    private void HandleGameplayInputLockRequested(string lockId)
    {
        string normalizedLockId = NormalizeGameplayInputLockId(lockId);
        if (string.IsNullOrEmpty(normalizedLockId))
            return;

        _gameplayInputLocks.Add(normalizedLockId);
        RefreshGameplayInputState();
    }

    private void HandleGameplayInputUnlockRequested(string lockId)
    {
        string normalizedLockId = NormalizeGameplayInputLockId(lockId);
        if (string.IsNullOrEmpty(normalizedLockId))
            return;

        _gameplayInputLocks.Remove(normalizedLockId);
        RefreshGameplayInputState();
    }

    private void RefreshGameplayInputState()
    {
        if (_inputReader == null)
            return;

        bool allowsMovementInput = AllowsMovementInput();
        bool uiBlockingGameplay = IsUiBlockingGameplay();

        if (allowsMovementInput)
        {
            _inputReader.SetMove(ReadCurrentMoveInput());
        }
        else
        {
            _inputReader.SetMove(Vector2.zero);
        }

        if (uiBlockingGameplay)
        {
            _inputReader.CancelGameplayInputState(clearMove: !allowsMovementInput, notifyGameplaySuppressed: true);
        }
        else if (!AllowsCombatInput())
        {
            _inputReader.CancelGameplayInputState(clearMove: false, notifyGameplaySuppressed: false);
        }
    }

    private Vector2 ReadCurrentMoveInput()
    {
        return _inputActions != null
            ? _inputActions.Player.Move.ReadValue<Vector2>()
            : Vector2.zero;
    }

    private bool AllowsMovementInput()
    {
        return !HasGameplayInputBlockers();
    }

    private bool AllowsInteractionInput()
    {
        return !HasGameplayInputBlockers();
    }

    private bool AllowsDashInput()
    {
        return !IsUiBlockingGameplay();
    }

    private bool AllowsCombatInput()
    {
        return !IsUiBlockingGameplay() && !IsCombatDisabledInCurrentScene();
    }

    private bool IsUiBlockingGameplay()
    {
        return HasGameplayInputBlockers();
    }

    private bool HasGameplayInputBlockers()
    {
        return _openBlockingScreens.Count > 0 || _gameplayInputLocks.Count > 0;
    }

    private static string NormalizeGameplayInputLockId(string lockId)
    {
        return string.IsNullOrWhiteSpace(lockId) ? string.Empty : lockId.Trim();
    }

    private bool IsCombatDisabledInCurrentScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (_combatDisabledSceneNames == null)
            return false;

        for (int i = 0; i < _combatDisabledSceneNames.Length; i++)
        {
            string configuredSceneName = _combatDisabledSceneNames[i];
            if (!string.IsNullOrWhiteSpace(configuredSceneName)
                && string.Equals(currentSceneName, configuredSceneName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsGameplayBlockingScreen(ScreenType screenType)
    {
        return screenType != ScreenType.None
            && screenType != ScreenType.HUD;
    }
}
