using System;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Registers Toris gameplay commands that Pixel Crushers dialogue can call from conversation scripts.
/// Keep these commands generic so dialogue content requests gameplay actions without owning gameplay logic.
/// Add one active instance in a scene that contains the Dialogue Manager.
/// Pixel Crushers scripts can call TorisOpenScreen, TorisCloseScreen, TorisReportFact, and TorisOpenQuestOffers.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersDialogueCommandBridge : MonoBehaviour
{
    private const string OpenScreenCommandName = "TorisOpenScreen";
    private const string CloseScreenCommandName = "TorisCloseScreen";
    private const string ReportFactCommandName = "TorisReportFact";
    private const string OpenQuestOffersCommandName = "TorisOpenQuestOffers";

    [Tooltip("Project UI event channel used to open or close Toris UI screens from dialogue commands.")]
    [SerializeField] private UIEventsSO _uiEvents;
    [Tooltip("Named commands for screens that need extra payloads, such as opening a shop with a specific NPC inventory.")]
    [SerializeField] private PixelCrushersDialogueScreenCommand[] _screenCommands = Array.Empty<PixelCrushersDialogueScreenCommand>();

#if UNITY_EDITOR
    [Tooltip("Logs dialogue command calls and invalid command names. Editor only.")]
    [SerializeField] private bool _debugCommands = true;
#endif

    private void OnEnable()
    {
        Lua.RegisterFunction(OpenScreenCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisOpenScreen(string.Empty)));
        Lua.RegisterFunction(CloseScreenCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisCloseScreen(string.Empty)));
        Lua.RegisterFunction(ReportFactCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisReportFact(string.Empty, string.Empty, string.Empty, 1D, string.Empty)));
        Lua.RegisterFunction(OpenQuestOffersCommandName, this, SymbolExtensions.GetMethodInfo(() => TorisOpenQuestOffers(string.Empty)));
    }

    private void OnDisable()
    {
        Lua.UnregisterFunction(OpenScreenCommandName);
        Lua.UnregisterFunction(CloseScreenCommandName);
        Lua.UnregisterFunction(ReportFactCommandName);
        Lua.UnregisterFunction(OpenQuestOffersCommandName);
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

    public void TorisOpenQuestOffers(string offerGroupId)
    {
        if (string.IsNullOrWhiteSpace(offerGroupId))
            return;

        if (!TryGetComponent(out PixelCrushersQuestOfferWindow offerWindow))
        {
            LogWarning($"Cannot open quest offer group '{offerGroupId}' because this bridge has no PixelCrushersQuestOfferWindow.");
            return;
        }

        offerWindow.Open(offerGroupId);
        LogDebug($"Opened quest offer group '{offerGroupId}'.");
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
