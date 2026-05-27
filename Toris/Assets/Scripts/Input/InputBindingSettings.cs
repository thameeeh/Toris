using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public struct InputBindingDisplayEntry
{
    public InputBindingDisplayEntry(
        string id,
        string displayName,
        string actionMapName,
        string actionName,
        int bindingIndex,
        string bindingLabel,
        bool hasOverride)
    {
        Id = id;
        DisplayName = displayName;
        ActionMapName = actionMapName;
        ActionName = actionName;
        BindingIndex = bindingIndex;
        BindingLabel = bindingLabel;
        HasOverride = hasOverride;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string ActionMapName { get; }
    public string ActionName { get; }
    public int BindingIndex { get; }
    public string BindingLabel { get; }
    public bool HasOverride { get; }
}

public static class InputBindingSettings
{
    private const string BindingOverridesKey = "input.binding_overrides";
    private const string KeyboardMouseGroup = "Keyboard&Mouse";
    private const string PlayerMap = "Player";
    private const string UiMap = "UI";

    private static readonly BindingDefinition[] BindingDefinitions =
    {
        new BindingDefinition("Move Up", PlayerMap, "Move", "<Keyboard>/w", "up"),
        new BindingDefinition("Move Down", PlayerMap, "Move", "<Keyboard>/s", "down"),
        new BindingDefinition("Move Left", PlayerMap, "Move", "<Keyboard>/a", "left"),
        new BindingDefinition("Move Right", PlayerMap, "Move", "<Keyboard>/d", "right"),
        new BindingDefinition("Attack", PlayerMap, "Attack", "<Mouse>/leftButton"),
        new BindingDefinition("Interact", PlayerMap, "Interact", "<Keyboard>/e"),
        new BindingDefinition("Dash", PlayerMap, "Sprint", "<Keyboard>/leftShift"),
        new BindingDefinition("Ability 1", PlayerMap, "Ability1", "<Keyboard>/q"),
        new BindingDefinition("Ability 2", PlayerMap, "Ability2", "<Keyboard>/r"),
        new BindingDefinition("Ability 3", PlayerMap, "Ability3", "<Keyboard>/z"),
        new BindingDefinition("Ability 4", PlayerMap, "Ability4", "<Keyboard>/x"),
        new BindingDefinition("Ability 5", PlayerMap, "Ability5", "<Keyboard>/c"),
        new BindingDefinition("Potion 1", PlayerMap, "Potion_1", "<Keyboard>/1"),
        new BindingDefinition("Potion 2", PlayerMap, "Potion_2", "<Keyboard>/2"),
        new BindingDefinition("Pause", PlayerMap, "Pause", "<Keyboard>/escape"),
        new BindingDefinition("Inventory", UiMap, "ToggleInventory", "<Keyboard>/i"),
        new BindingDefinition("Skills", UiMap, "ToggleSkills", "<Keyboard>/u"),
        new BindingDefinition("Quest Journal", UiMap, "ToggleQuestJournal", "<Keyboard>/j"),
        new BindingDefinition("Quick Save", UiMap, "QuickSave", "<Keyboard>/f5"),
        new BindingDefinition("Quick Load", UiMap, "QuickLoad", "<Keyboard>/f9")
    };

    public static event Action OnBindingsChanged;

    public static void ApplyTo(InputSystem_Actions actions)
    {
        if (actions == null)
        {
            return;
        }

        bool playerWasEnabled = actions.Player.enabled;
        bool uiWasEnabled = actions.UI.enabled;

        if (playerWasEnabled)
        {
            actions.Player.Disable();
        }

        if (uiWasEnabled)
        {
            actions.UI.Disable();
        }

        try
        {
            actions.asset.RemoveAllBindingOverrides();

            string overridesJson = PlayerPrefs.GetString(BindingOverridesKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(overridesJson))
            {
                try
                {
                    actions.asset.LoadBindingOverridesFromJson(overridesJson);
                }
                catch (Exception)
                {
                    PlayerPrefs.DeleteKey(BindingOverridesKey);
                    PlayerPrefs.Save();
                }
            }
        }
        finally
        {
            if (playerWasEnabled)
            {
                actions.Player.Enable();
            }

            if (uiWasEnabled)
            {
                actions.UI.Enable();
            }
        }
    }

    public static void SaveOverrides(InputSystem_Actions actions)
    {
        if (actions == null)
        {
            return;
        }

        if (HasAnyOverrides(actions))
        {
            PlayerPrefs.SetString(BindingOverridesKey, actions.asset.SaveBindingOverridesAsJson());
        }
        else
        {
            PlayerPrefs.DeleteKey(BindingOverridesKey);
        }

        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static void ClearOverrides()
    {
        PlayerPrefs.DeleteKey(BindingOverridesKey);
        PlayerPrefs.Save();
        OnBindingsChanged?.Invoke();
    }

    public static List<InputBindingDisplayEntry> GetDisplayEntries(InputSystem_Actions actions)
    {
        List<InputBindingDisplayEntry> entries = new List<InputBindingDisplayEntry>();
        if (actions == null)
        {
            return entries;
        }

        for (int i = 0; i < BindingDefinitions.Length; i++)
        {
            BindingDefinition definition = BindingDefinitions[i];
            if (!TryResolveBinding(actions, definition, out InputAction action, out int bindingIndex))
            {
                continue;
            }

            InputBinding binding = action.bindings[bindingIndex];
            entries.Add(new InputBindingDisplayEntry(
                CreateEntryId(definition.ActionMapName, definition.ActionName, bindingIndex),
                definition.DisplayName,
                definition.ActionMapName,
                definition.ActionName,
                bindingIndex,
                GetBindingDisplayString(action, bindingIndex),
                HasOverride(binding)));
        }

        return entries;
    }

    public static bool TryFindDisplayEntry(IList<InputBindingDisplayEntry> entries, string entryId, out InputBindingDisplayEntry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Id, entryId, StringComparison.Ordinal))
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = default;
        return false;
    }

    public static bool TryFindDuplicateBinding(
        InputSystem_Actions actions,
        IList<InputBindingDisplayEntry> entries,
        InputBindingDisplayEntry targetEntry,
        out InputBindingDisplayEntry duplicateEntry)
    {
        duplicateEntry = default;

        if (actions == null || entries == null)
        {
            return false;
        }

        InputAction targetAction = FindAction(actions, targetEntry.ActionMapName, targetEntry.ActionName);
        if (!TryGetComparableBindingPath(targetAction, targetEntry.BindingIndex, out string targetPath))
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            InputBindingDisplayEntry candidateEntry = entries[i];
            if (string.Equals(candidateEntry.Id, targetEntry.Id, StringComparison.Ordinal))
            {
                continue;
            }

            InputAction candidateAction = FindAction(actions, candidateEntry.ActionMapName, candidateEntry.ActionName);
            if (!TryGetComparableBindingPath(candidateAction, candidateEntry.BindingIndex, out string candidatePath))
            {
                continue;
            }

            if (string.Equals(targetPath, candidatePath, StringComparison.Ordinal))
            {
                duplicateEntry = candidateEntry;
                return true;
            }
        }

        return false;
    }

    public static InputAction FindAction(InputSystem_Actions actions, string actionMapName, string actionName)
    {
        if (actions == null || string.IsNullOrWhiteSpace(actionMapName) || string.IsNullOrWhiteSpace(actionName))
        {
            return null;
        }

        InputActionMap actionMap = actions.asset.FindActionMap(actionMapName, throwIfNotFound: false);
        return actionMap?.FindAction(actionName, throwIfNotFound: false);
    }

    public static string GetPrimaryKeyboardMouseDisplayString(InputSystem_Actions actions, string actionMapName, string actionName)
    {
        InputAction action = FindAction(actions, actionMapName, actionName);
        if (action == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!binding.isComposite && !binding.isPartOfComposite && IsKeyboardMouseBinding(binding))
            {
                return GetBindingDisplayString(action, i);
            }
        }

        return string.Empty;
    }

    private static bool TryResolveBinding(
        InputSystem_Actions actions,
        BindingDefinition definition,
        out InputAction action,
        out int bindingIndex)
    {
        action = FindAction(actions, definition.ActionMapName, definition.ActionName);
        bindingIndex = -1;

        if (action == null)
        {
            return false;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (string.Equals(binding.path, definition.PreferredPath, StringComparison.OrdinalIgnoreCase))
            {
                bindingIndex = i;
                return true;
            }
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (IsMatchingCompositePart(binding, definition.CompositePartName))
            {
                bindingIndex = i;
                return true;
            }
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!binding.isComposite && !binding.isPartOfComposite && IsKeyboardMouseBinding(binding))
            {
                bindingIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetComparableBindingPath(InputAction action, int bindingIndex, out string path)
    {
        path = string.Empty;
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return false;
        }

        InputBinding binding = action.bindings[bindingIndex];
        if (!IsKeyboardMouseBinding(binding))
        {
            return false;
        }

        path = NormalizeBindingPath(binding.effectivePath);
        return !string.IsNullOrEmpty(path);
    }

    private static bool IsMatchingCompositePart(InputBinding binding, string compositePartName)
    {
        return !string.IsNullOrWhiteSpace(compositePartName)
            && binding.isPartOfComposite
            && IsKeyboardMouseBinding(binding)
            && string.Equals(binding.name, compositePartName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsKeyboardMouseBinding(InputBinding binding)
    {
        return ContainsKeyboardMouseGroup(binding.groups)
            || StartsWithDevice(binding.effectivePath, "<Keyboard>")
            || StartsWithDevice(binding.effectivePath, "<Mouse>");
    }

    private static bool ContainsKeyboardMouseGroup(string groups)
    {
        return !string.IsNullOrWhiteSpace(groups)
            && groups.IndexOf(KeyboardMouseGroup, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool StartsWithDevice(string path, string devicePrefix)
    {
        return !string.IsNullOrWhiteSpace(path)
            && path.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasAnyOverrides(InputSystem_Actions actions)
    {
        foreach (InputBinding binding in actions.asset.bindings)
        {
            if (HasOverride(binding))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetBindingDisplayString(InputAction action, int bindingIndex)
    {
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return string.Empty;
        }

        string displayString = action.GetBindingDisplayString(bindingIndex);
        if (string.IsNullOrWhiteSpace(displayString))
        {
            displayString = action.bindings[bindingIndex].effectivePath;
        }

        return CleanDisplayString(displayString);
    }

    private static bool HasOverride(InputBinding binding)
    {
        return !string.IsNullOrEmpty(binding.overridePath)
            || !string.IsNullOrEmpty(binding.overrideProcessors)
            || !string.IsNullOrEmpty(binding.overrideInteractions);
    }

    private static string CleanDisplayString(string displayString)
    {
        if (string.IsNullOrWhiteSpace(displayString))
        {
            return string.Empty;
        }

        string cleaned = displayString.Trim();
        int slashIndex = cleaned.LastIndexOf('/');
        if (slashIndex >= 0 && slashIndex < cleaned.Length - 1)
        {
            cleaned = cleaned.Substring(slashIndex + 1);
        }

        return cleaned
            .Replace("leftButton", "LMB")
            .Replace("rightButton", "RMB")
            .Replace("middleButton", "MMB")
            .Replace("leftShift", "Left Shift")
            .Replace("escape", "Escape")
            .Replace("enter", "Enter");
    }

    private static string NormalizeBindingPath(string path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().ToLowerInvariant();
    }

    private static string CreateEntryId(string actionMapName, string actionName, int bindingIndex)
    {
        return $"{actionMapName}/{actionName}/{bindingIndex}";
    }

    private struct BindingDefinition
    {
        public BindingDefinition(string displayName, string actionMapName, string actionName, string preferredPath, string compositePartName = null)
        {
            DisplayName = displayName;
            ActionMapName = actionMapName;
            ActionName = actionName;
            PreferredPath = preferredPath;
            CompositePartName = compositePartName;
        }

        public string DisplayName { get; }
        public string ActionMapName { get; }
        public string ActionName { get; }
        public string PreferredPath { get; }
        public string CompositePartName { get; }
    }
}
