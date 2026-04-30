using System;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Registers Toris gameplay commands that Pixel Crushers dialogue can call from conversation scripts.
/// Keep these commands generic so dialogue content requests gameplay actions without owning gameplay logic.
/// Add one active instance in a scene that contains the Dialogue Manager.
/// Pixel Crushers scripts can call TorisOpenScreen, TorisCloseScreen, TorisReportFact, and TorisOpenQuestJournal.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersDialogueCommandBridge : MonoBehaviour
{
    private const string OpenScreenCommandName = "TorisOpenScreen";
    private const string CloseScreenCommandName = "TorisCloseScreen";
    private const string ReportFactCommandName = "TorisReportFact";
    private const string OpenQuestJournalCommandName = "TorisOpenQuestJournal";

    [Tooltip("Project UI event channel used to open or close Toris UI screens from dialogue commands.")]
    [SerializeField] private UIEventsSO _uiEvents;
    [Tooltip("Named commands for screens that need extra payloads, such as opening a shop with a specific NPC inventory.")]
    [SerializeField] private PixelCrushersDialogueScreenCommand[] _screenCommands = Array.Empty<PixelCrushersDialogueScreenCommand>();
    [Tooltip("Optional scene instance of the Pixel Crushers quest journal.")]
    [SerializeField] private PixelCrushersQuestJournalWindow _questJournalWindow;
    [Tooltip("Optional quest journal prefab to instantiate if no scene instance is assigned.")]
    [SerializeField] private PixelCrushersQuestJournalWindow _questJournalPrefab;
    [Tooltip("Optional Canvas parent for the runtime quest journal prefab. Leave empty to create a dedicated runtime Canvas.")]
    [SerializeField] private Canvas _questJournalCanvas;
    [Tooltip("Create a dedicated runtime Canvas when using a quest journal prefab without a scene Canvas parent.")]
    [SerializeField] private bool _createRuntimeQuestJournalCanvas = true;
    [Tooltip("Sorting order for the dedicated runtime quest journal Canvas.")]
    [SerializeField] private int _runtimeQuestJournalSortingOrder = 200;
    [Header("Gameplay Input Locks")]
    [Tooltip("When enabled, active Pixel Crushers conversations freeze Toris gameplay input while keeping dialogue UI input usable.")]
    [SerializeField] private bool _lockGameplayInputDuringDialogue = true;
    [Tooltip("Named gameplay input lock used while Pixel Crushers dialogue is active.")]
    [SerializeField] private string _dialogueGameplayInputLockId = "PixelCrushersDialogue";

#if UNITY_EDITOR
    [Tooltip("Logs dialogue command calls and invalid command names. Editor only.")]
    [SerializeField] private bool _debugCommands = true;
#endif

    private PixelCrushersQuestJournalWindow _runtimeQuestJournal;
    private Canvas _runtimeQuestJournalCanvas;
    private bool _dialogueEventsSubscribed;
    private bool _dialogueInputLocked;

    private void OnEnable()
    {
        Lua.RegisterFunction(OpenScreenCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisOpenScreen(string.Empty)));
        Lua.RegisterFunction(CloseScreenCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisCloseScreen(string.Empty)));
        Lua.RegisterFunction(ReportFactCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisReportFact(string.Empty, string.Empty, string.Empty, 1D, string.Empty)));
        Lua.RegisterFunction(OpenQuestJournalCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisOpenQuestJournal(string.Empty)));

        if (_uiEvents != null)
            _uiEvents.OnQuestJournalOpenRequested += HandleQuestJournalOpenRequested;

        TrySubscribeDialogueEvents();
    }

    private void Start()
    {
        TrySubscribeDialogueEvents();
    }

    private void OnDisable()
    {
        ReleaseDialogueGameplayInputLock();
        UnsubscribeDialogueEvents();

        if (_uiEvents != null)
            _uiEvents.OnQuestJournalOpenRequested -= HandleQuestJournalOpenRequested;

        Lua.UnregisterFunction(OpenScreenCommandName);
        Lua.UnregisterFunction(CloseScreenCommandName);
        Lua.UnregisterFunction(ReportFactCommandName);
        Lua.UnregisterFunction(OpenQuestJournalCommandName);
    }

    public void TorisOpenScreen(string commandOrScreenName)
    {
        if (_uiEvents == null || string.IsNullOrWhiteSpace(commandOrScreenName))
            return;

        if (TryFindScreenCommand(commandOrScreenName, out PixelCrushersDialogueScreenCommand command))
        {
            _uiEvents.OnRequestOpen?.Invoke(command.ScreenType, command.ResolvePayload());
            LogDebug($"Opened screen command '{command.CommandId}' -> {command.ScreenType}.");
            return;
        }

        if (!Enum.TryParse(commandOrScreenName, true, out ScreenType screenType) || screenType == ScreenType.None)
        {
            LogWarning($"Unknown screen command or ScreenType '{commandOrScreenName}'.");
            return;
        }

        _uiEvents.OnRequestOpen?.Invoke(screenType, null);
        LogDebug($"Opened screen '{screenType}' with no payload.");
    }

    public void TorisCloseScreen(string screenName)
    {
        if (_uiEvents == null || string.IsNullOrWhiteSpace(screenName))
            return;

        if (!Enum.TryParse(screenName, true, out ScreenType screenType) || screenType == ScreenType.None)
        {
            LogWarning($"Unknown ScreenType '{screenName}'.");
            return;
        }

        _uiEvents.OnRequestClose?.Invoke(screenType);
        LogDebug($"Closed screen '{screenType}'.");
    }

    public void TorisReportFact(string factType, string exactId, string typeOrTag, double amount, string contextId)
    {
        if (!Enum.TryParse(factType, true, out QuestFactType parsedFactType))
        {
            LogWarning($"Unknown QuestFactType '{factType}'.");
            return;
        }

        int roundedAmount = Mathf.Max(1, Mathf.RoundToInt((float)amount));
        PixelCrushersQuestFactReporter.Report(new QuestFact(parsedFactType, exactId, typeOrTag, roundedAmount, contextId));
    }

    public void TorisOpenQuestJournal(string mode)
    {
        PixelCrushersQuestJournalWindow questJournal = ResolveQuestJournalWindow();
        if (questJournal == null)
        {
            LogWarning("Cannot open quest journal because no scene instance or prefab is assigned.");
            return;
        }

        ParseQuestJournalMode(mode, out string normalizedMode, out string questGroupFilter);
        if (IsQuestJournalMode(normalizedMode, "Available") || IsQuestJournalMode(normalizedMode, "Jobs") || IsQuestJournalMode(normalizedMode, "Grantable"))
        {
            questJournal.OpenAvailableJobs(questGroupFilter);
            LogDebug(string.IsNullOrWhiteSpace(questGroupFilter)
                ? "Opened quest journal in Available Jobs mode."
                : $"Opened quest journal in Available Jobs mode for group '{questGroupFilter}'.");
            return;
        }

        if (IsQuestJournalMode(normalizedMode, "Active"))
        {
            questJournal.OpenQuestBook();
            LogDebug("Opened quest journal in Active mode.");
            return;
        }

        if (IsQuestJournalMode(normalizedMode, "Completed"))
        {
            questJournal.OpenQuestBook();
            questJournal.ClickShowCompletedQuests(null);
            LogDebug("Opened quest journal in Completed mode.");
            return;
        }

        LogWarning($"Unknown quest journal mode '{mode}'. Use Available, Active, or Completed.");
    }

    private PixelCrushersQuestJournalWindow ResolveQuestJournalWindow()
    {
        if (_questJournalWindow != null)
            return _questJournalWindow;

        if (_runtimeQuestJournal != null)
            return _runtimeQuestJournal;

        if (_questJournalPrefab == null)
            return null;

        Transform journalParent = ResolveQuestJournalParent();
        _runtimeQuestJournal = journalParent != null
            ? Instantiate(_questJournalPrefab, journalParent, false)
            : Instantiate(_questJournalPrefab);
        _runtimeQuestJournal.name = _questJournalPrefab.name;
        return _runtimeQuestJournal;
    }

    private Transform ResolveQuestJournalParent()
    {
        if (_questJournalCanvas != null)
            return _questJournalCanvas.transform;

        if (!_createRuntimeQuestJournalCanvas)
            return null;

        if (_runtimeQuestJournalCanvas != null)
            return _runtimeQuestJournalCanvas.transform;

        GameObject canvasObject = new GameObject("Runtime Quest Journal Canvas");
        _runtimeQuestJournalCanvas = canvasObject.AddComponent<Canvas>();
        _runtimeQuestJournalCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _runtimeQuestJournalCanvas.sortingOrder = _runtimeQuestJournalSortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        LogDebug("Created runtime Canvas for the quest journal prefab.");
        return _runtimeQuestJournalCanvas.transform;
    }

    private static bool IsQuestJournalMode(string value, string expected)
    {
        return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static void ParseQuestJournalMode(string mode, out string normalizedMode, out string questGroupFilter)
    {
        normalizedMode = string.IsNullOrWhiteSpace(mode) ? "Active" : mode.Trim();
        questGroupFilter = string.Empty;

        int separatorIndex = normalizedMode.IndexOf(':');
        if (separatorIndex < 0)
            separatorIndex = normalizedMode.IndexOf('|');

        if (separatorIndex < 0)
            return;

        questGroupFilter = normalizedMode.Substring(separatorIndex + 1).Trim();
        normalizedMode = normalizedMode.Substring(0, separatorIndex).Trim();
    }

    private bool TryFindScreenCommand(string commandId, out PixelCrushersDialogueScreenCommand command)
    {
        command = null;

        if (_screenCommands == null)
            return false;

        for (int i = 0; i < _screenCommands.Length; i++)
        {
            PixelCrushersDialogueScreenCommand candidate = _screenCommands[i];
            if (candidate == null || !candidate.Matches(commandId))
                continue;

            command = candidate;
            return true;
        }

        return false;
    }

    private void HandleQuestJournalOpenRequested(string mode)
    {
        TorisOpenQuestJournal(mode);
    }

    private void TrySubscribeDialogueEvents()
    {
        if (!_lockGameplayInputDuringDialogue || _dialogueEventsSubscribed || !DialogueManager.hasInstance)
            return;

        DialogueManager.instance.conversationStarted += HandleConversationStarted;
        DialogueManager.instance.conversationEnded += HandleConversationEnded;
        _dialogueEventsSubscribed = true;
    }

    private void UnsubscribeDialogueEvents()
    {
        if (!_dialogueEventsSubscribed)
            return;

        if (DialogueManager.hasInstance)
        {
            DialogueManager.instance.conversationStarted -= HandleConversationStarted;
            DialogueManager.instance.conversationEnded -= HandleConversationEnded;
        }

        _dialogueEventsSubscribed = false;
    }

    private void HandleConversationStarted(Transform actor)
    {
        RequestDialogueGameplayInputLock();
    }

    private void HandleConversationEnded(Transform actor)
    {
        ReleaseDialogueGameplayInputLock();
    }

    private void RequestDialogueGameplayInputLock()
    {
        if (_uiEvents == null || _dialogueInputLocked || string.IsNullOrWhiteSpace(_dialogueGameplayInputLockId))
            return;

        _uiEvents.OnGameplayInputLockRequested?.Invoke(_dialogueGameplayInputLockId);
        _dialogueInputLocked = true;
    }

    private void ReleaseDialogueGameplayInputLock()
    {
        if (_uiEvents == null || !_dialogueInputLocked || string.IsNullOrWhiteSpace(_dialogueGameplayInputLockId))
            return;

        _uiEvents.OnGameplayInputUnlockRequested?.Invoke(_dialogueGameplayInputLockId);
        _dialogueInputLocked = false;
    }

    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        if (_debugCommands)
            Debug.Log($"[PixelCrushersDialogueCommandBridge] {message}", this);
#endif
    }

    private void LogWarning(string message)
    {
#if UNITY_EDITOR
        if (_debugCommands)
            Debug.LogWarning($"[PixelCrushersDialogueCommandBridge] {message}", this);
#endif
    }
}

[Serializable]
public class PixelCrushersDialogueScreenCommand
{
    [Tooltip("Name used by dialogue scripts. Example Pixel Crushers script: TorisOpenScreen(\"SmithShop\").")]
    public string CommandId = string.Empty;
    [Tooltip("Toris UI screen to open when this command is called.")]
    public ScreenType ScreenType = ScreenType.None;
    [Tooltip("Enable for vendor/shop screens that need a specific InventoryManager payload.")]
    public bool PassInventoryPayload = false;
    [Tooltip("Inventory payload sent with the screen open request. Usually the NPC shop InventoryManager.")]
    public InventoryManager InventoryPayload;

    public bool Matches(string commandId)
    {
        return !string.IsNullOrWhiteSpace(CommandId)
               && string.Equals(CommandId, commandId, StringComparison.Ordinal);
    }

    public object ResolvePayload()
    {
        return PassInventoryPayload ? InventoryPayload : null;
    }
}
