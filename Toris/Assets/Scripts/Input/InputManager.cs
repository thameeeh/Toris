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
    private readonly HashSet<string> _gameplayInputLocks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    // Per-capability locks let guided flows suppress only unrelated inputs, such as inventory
    // during the first movement prompt. The values are owner ids so locks can overlap safely.
    private readonly Dictionary<GameplayInputCapability, HashSet<string>> _gameplayCapabilityLocks =
        new Dictionary<GameplayInputCapability, HashSet<string>>();
    // Shared lock id used by the death screen flow to suppress gameplay and hotkey UI.
    private const string DeathGameplayLockId = "Death";
    // Shared lock id used by the tutorial runtime; input only honors the lock, it does not drive tutorial flow.
    private const string TutorialGameplayLockId = "Tutorial";

    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private ItemPickEventSO _itemPicker;

    [Header("UI Events")]
    [SerializeField] private UIEventsSO _uiEvents;

    [Header("Gameplay Input Policy")]
    [Tooltip("Combat-like inputs are disabled in these scenes, but movement and interaction can still stay active.")]
    [SerializeField] private string[] _combatDisabledSceneNames = { "MainArea" };

    private InputSystem_Actions _inputActions;
    private int _lastFrameEscapeProcessed = -1;

    private void OnEnable()
    {
        _inputActions = new InputSystem_Actions();
        // Settings rebinding hook: apply saved overrides before gameplay starts reading actions.
        InputBindingSettings.ApplyTo(_inputActions);
        _inputActions.Enable();
        _inputActions.Player.SetCallbacks(this);
        _inputActions.UI.SetCallbacks(this);
        InputBindingSettings.OnBindingsChanged += HandleInputBindingsChanged;

        if (_uiEvents != null)
        {
            _uiEvents.OnScreenOpen += HandleScreenOpened;
            _uiEvents.OnScreenClose += HandleScreenClosed;
            _uiEvents.OnGameplayInputLockRequested += HandleGameplayInputLockRequested;
            _uiEvents.OnGameplayInputUnlockRequested += HandleGameplayInputUnlockRequested;
            _uiEvents.OnGameplayCapabilityLockRequested += HandleGameplayCapabilityLockRequested;
            _uiEvents.OnGameplayCapabilityUnlockRequested += HandleGameplayCapabilityUnlockRequested;
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;
        RefreshGameplayInputState();
    }

    private void OnDisable()
    {
        InputBindingSettings.OnBindingsChanged -= HandleInputBindingsChanged;

        if (_uiEvents != null)
        {
            _uiEvents.OnScreenOpen -= HandleScreenOpened;
            _uiEvents.OnScreenClose -= HandleScreenClosed;
            _uiEvents.OnGameplayInputLockRequested -= HandleGameplayInputLockRequested;
            _uiEvents.OnGameplayInputUnlockRequested -= HandleGameplayInputUnlockRequested;
            _uiEvents.OnGameplayCapabilityLockRequested -= HandleGameplayCapabilityLockRequested;
            _uiEvents.OnGameplayCapabilityUnlockRequested -= HandleGameplayCapabilityUnlockRequested;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        _openBlockingScreens.Clear();
        _gameplayInputLocks.Clear();
        _gameplayCapabilityLocks.Clear();

        if (_inputActions != null)
        {
            _inputActions.Player.SetCallbacks(null);
            _inputActions.UI.SetCallbacks(null);
            _inputActions.Disable();
        }
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
    public void OnPause(InputAction.CallbackContext context) 
    {
        HandleEscapeLogic(context);
    }
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

    public void OnPotion_1(InputAction.CallbackContext context)
    {
        if (context.performed
            && !IsDeathInputLocked()
            && !IsTutorialInputLocked()
            && !IsCapabilityLocked(GameplayInputCapability.PotionHotkeys))
        {
            _inputReader.OnPotion1Pressed?.Invoke();
        }
    }

    public void OnPotion_2(InputAction.CallbackContext context)
    {
        if (context.performed
            && !IsDeathInputLocked()
            && !IsTutorialInputLocked()
            && !IsCapabilityLocked(GameplayInputCapability.PotionHotkeys))
        {
            _inputReader.OnPotion2Pressed?.Invoke();
        }
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
        HandleEscapeLogic(context);
    }
    public void OnPoint(InputAction.CallbackContext context) { }
    public void OnClick(InputAction.CallbackContext context) { }
    public void OnRightClick(InputAction.CallbackContext context) { }
    public void OnMiddleClick(InputAction.CallbackContext context) { }
    public void OnScrollWheel(InputAction.CallbackContext context) { }
    public void OnTrackedDevicePosition(InputAction.CallbackContext context) { }
    public void OnTrackedDeviceOrientation(InputAction.CallbackContext context) { }

    /// <summary>
    /// Centralized logic for the Escape key. 
    /// Priority: 1. Close any open UI windows. 2. If no windows are open, open the Pause Menu.
    /// Uses frame-level coordination to prevent race conditions between Pause and Cancel actions.
    /// </summary>
    private void HandleEscapeLogic(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Prevent processing both 'Pause' and 'Cancel' bindings in the same frame
        if (_lastFrameEscapeProcessed == Time.frameCount) return;
        _lastFrameEscapeProcessed = Time.frameCount;

        if (IsDeathInputLocked())
            return;

        if (IsTutorialInputLocked())
            return;

        // Settings can sit on top of Pause, so Escape closes that modal before broad UI teardown.
        if (_openBlockingScreens.Contains(ScreenType.SettingsModal))
        {
            _uiEvents.OnRequestClose?.Invoke(ScreenType.SettingsModal);
            return;
        }

        if (HasGameplayInputBlockers())
        {
            // If UI is open, we close it first. This satisfies the "Press ESC once to close" requirement.
            _uiEvents.OnRequestCloseAll?.Invoke();
        }
        else
        {
            // If the screen is clear, we proceed to open the Pause Menu.
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.PauseMenu, null);
        }
    }

    public void OnToggleInventory(InputAction.CallbackContext context)
    {
        if (context.performed
            && AllowsUiToggleInput()
            && !IsCapabilityLocked(GameplayInputCapability.Inventory))
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.Inventory, null);
        }
    }

    public void OnToggleMage(InputAction.CallbackContext context)
    {
        // Intentionally no-op: vendor screens require an NPC inventory payload from interaction.
        // Opening here with null context can show an empty or stale shop.
    }

    public void OnToggleSkills(InputAction.CallbackContext context)
    {
        if (context.performed
            && AllowsUiToggleInput()
            && !IsCapabilityLocked(GameplayInputCapability.Skills))
        {
            _uiEvents.OnRequestOpen?.Invoke(ScreenType.Skills, null);
        }
    }

    public void OnToggleSmith(InputAction.CallbackContext context)
    {
        // Intentionally no-op: vendor screens require an NPC inventory payload from interaction.
        // Opening here with null context can show an empty or stale shop.
    }

    public void OnToggleQuestJournal(InputAction.CallbackContext context)
    {
        if (context.performed
            && !HasGameplayInputBlockers()
            && !IsCapabilityLocked(GameplayInputCapability.QuestJournal))
        {
            _uiEvents.OnQuestJournalOpenRequested?.Invoke("Active");
        }
    }

    public void OnQuickSave(InputAction.CallbackContext context)
    {
        if (context.performed
            && !IsDeathInputLocked()
            && !IsTutorialInputLocked()
            && !IsCapabilityLocked(GameplayInputCapability.QuickSaveLoad))
        {
            _uiEvents?.OnQuickSaveRequested?.Invoke();
        }
    }

    public void OnQuickLoad(InputAction.CallbackContext context)
    {
        if (context.performed
            && !IsDeathInputLocked()
            && !IsTutorialInputLocked()
            && !IsCapabilityLocked(GameplayInputCapability.QuickSaveLoad))
        {
            _uiEvents?.OnQuickLoadRequested?.Invoke();
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _openBlockingScreens.Clear();
        _gameplayInputLocks.Clear();
        _gameplayCapabilityLocks.Clear();
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

    private void HandleGameplayCapabilityLockRequested(GameplayInputCapability capability, string lockId)
    {
        // InputManager only enforces these requests; tutorial/combat/UI systems decide when a
        // capability should be gated so this class does not become a tutorial state machine.
        string normalizedLockId = NormalizeGameplayInputLockId(lockId);
        if (string.IsNullOrEmpty(normalizedLockId))
            return;

        if (!_gameplayCapabilityLocks.TryGetValue(capability, out HashSet<string> locks))
        {
            locks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _gameplayCapabilityLocks[capability] = locks;
        }

        locks.Add(normalizedLockId);
        RefreshGameplayInputState();
    }

    private void HandleGameplayCapabilityUnlockRequested(GameplayInputCapability capability, string lockId)
    {
        string normalizedLockId = NormalizeGameplayInputLockId(lockId);
        if (string.IsNullOrEmpty(normalizedLockId))
            return;

        if (!_gameplayCapabilityLocks.TryGetValue(capability, out HashSet<string> locks))
            return;

        locks.Remove(normalizedLockId);
        if (locks.Count == 0)
            _gameplayCapabilityLocks.Remove(capability);

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

    private void HandleInputBindingsChanged()
    {
        // Settings rebinding hook: keep the live gameplay input instance synced.
        InputBindingSettings.ApplyTo(_inputActions);
        RefreshGameplayInputState();
    }

    private bool AllowsMovementInput()
    {
        return !HasGameplayInputBlockers()
               && !IsCapabilityLocked(GameplayInputCapability.Movement);
    }

    private bool AllowsInteractionInput()
    {
        return !HasGameplayInputBlockers()
               && !IsCapabilityLocked(GameplayInputCapability.Interaction);
    }

    private bool AllowsDashInput()
    {
        return !IsUiBlockingGameplay()
               && !IsCapabilityLocked(GameplayInputCapability.Movement);
    }

    private bool AllowsCombatInput()
    {
        return !IsUiBlockingGameplay()
               && !IsCapabilityLocked(GameplayInputCapability.Combat)
               && !IsCombatDisabledInCurrentScene();
    }

    private bool IsUiBlockingGameplay()
    {
        return HasGameplayInputBlockers();
    }

    private bool HasGameplayInputBlockers()
    {
        return _openBlockingScreens.Count > 0 || _gameplayInputLocks.Count > 0;
    }

    private bool IsCapabilityLocked(GameplayInputCapability capability)
    {
        return _gameplayCapabilityLocks.TryGetValue(capability, out HashSet<string> locks)
               && locks.Count > 0;
    }

    private bool AllowsUiToggleInput()
    {
        return !IsDeathInputLocked() && !IsTutorialInputLocked();
    }

    private bool IsDeathInputLocked()
    {
        return _openBlockingScreens.Contains(ScreenType.DeathScreen)
            || _gameplayInputLocks.Contains(DeathGameplayLockId);
    }

    private bool IsTutorialInputLocked()
    {
        return _gameplayInputLocks.Contains(TutorialGameplayLockId);
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
