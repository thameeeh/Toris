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
        string controlSchemeName,
        string bindingLabel,
        bool hasOverride)
    {
        Id = id;
        DisplayName = displayName;
        ActionMapName = actionMapName;
        ActionName = actionName;
        BindingIndex = bindingIndex;
        ControlSchemeName = controlSchemeName;
        BindingLabel = bindingLabel;
        HasOverride = hasOverride;
    }

    public string Id { get; }
    public string DisplayName { get; }
    public string ActionMapName { get; }
    public string ActionName { get; }
    public int BindingIndex { get; }
    public string ControlSchemeName { get; }
    public string BindingLabel { get; }
    public bool HasOverride { get; }
}

public static class InputBindingSettings
{
    private const string BindingOverridesKey = "input.binding_overrides";
    private const string KeyboardMouseGroup = "Keyboard&Mouse";
    private const string GamepadGroup = "Gamepad";
    private const string PlayerMap = "Player";
    private const string UiMap = "UI";

    private static readonly BindingDefinition[] KeyboardMouseBindingDefinitions =
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
        new BindingDefinition("Quest Journal", UiMap, "ToggleQuestJournal", "<Keyboard>/j")
    };

    private static readonly BindingDefinition[] GamepadBindingDefinitions =
    {
        new BindingDefinition("Move", PlayerMap, "Move", "<Gamepad>/leftStick", null, GamepadGroup),
        new BindingDefinition("Attack", PlayerMap, "Attack", "<Gamepad>/rightTrigger", null, GamepadGroup),
        new BindingDefinition("Interact", PlayerMap, "Interact", "<Gamepad>/buttonSouth", null, GamepadGroup),
        new BindingDefinition("Dash", PlayerMap, "Sprint", "<Gamepad>/buttonEast", null, GamepadGroup),
        new BindingDefinition("Ability 1", PlayerMap, "Ability1", "<Gamepad>/rightShoulder", null, GamepadGroup),
        new BindingDefinition("Ability 2", PlayerMap, "Ability2", "<Gamepad>/leftShoulder", null, GamepadGroup),
        new BindingDefinition("Ability 3", PlayerMap, "Ability3", "<Gamepad>/dpad/up", null, GamepadGroup),
        new BindingDefinition("Ability 4", PlayerMap, "Ability4", "<Gamepad>/dpad/right", null, GamepadGroup),
        new BindingDefinition("Ability 5", PlayerMap, "Ability5", "<Gamepad>/dpad/down", null, GamepadGroup),
        new BindingDefinition("Potion 1", PlayerMap, "Potion_1", "<Gamepad>/dpad/left", null, GamepadGroup),
        new BindingDefinition("Potion 2", PlayerMap, "Potion_2", "<Gamepad>/leftTrigger", null, GamepadGroup),
        new BindingDefinition("Pause", PlayerMap, "Pause", "<Gamepad>/startButton", null, GamepadGroup),
        new BindingDefinition("Inventory", UiMap, "ToggleInventory", "<Gamepad>/selectButton", null, GamepadGroup),
        new BindingDefinition("Skills", UiMap, "ToggleSkills", "<Gamepad>/buttonNorth", null, GamepadGroup),
        new BindingDefinition("Quest Journal", UiMap, "ToggleQuestJournal", "<Gamepad>/rightStickPress", null, GamepadGroup)
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

        AddDisplayEntries(actions, KeyboardMouseBindingDefinitions, entries);
        AddDisplayEntries(actions, GamepadBindingDefinitions, entries);

        return entries;
    }

    public static bool IsGamepadControlScheme(string controlSchemeName)
    {
        return string.Equals(controlSchemeName, GamepadGroup, StringComparison.OrdinalIgnoreCase);
    }

    private static void AddDisplayEntries(
        InputSystem_Actions actions,
        BindingDefinition[] definitions,
        List<InputBindingDisplayEntry> entries)
    {
        for (int i = 0; i < definitions.Length; i++)
        {
            BindingDefinition definition = definitions[i];
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
                definition.ControlSchemeName,
                GetBindingDisplayString(action, bindingIndex),
                HasOverride(binding)));
        }
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
        if (!TryGetComparableBindingPath(targetAction, targetEntry.BindingIndex, targetEntry.ControlSchemeName, out string targetPath))
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

            if (!string.Equals(candidateEntry.ControlSchemeName, targetEntry.ControlSchemeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            InputAction candidateAction = FindAction(actions, candidateEntry.ActionMapName, candidateEntry.ActionName);
            if (!TryGetComparableBindingPath(candidateAction, candidateEntry.BindingIndex, candidateEntry.ControlSchemeName, out string candidatePath))
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
            if (IsMatchingCompositePart(binding, definition.CompositePartName, definition.ControlSchemeName))
            {
                bindingIndex = i;
                return true;
            }
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding binding = action.bindings[i];
            if (!binding.isComposite && !binding.isPartOfComposite && IsBindingInControlScheme(binding, definition.ControlSchemeName))
            {
                bindingIndex = i;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetComparableBindingPath(
        InputAction action,
        int bindingIndex,
        string controlSchemeName,
        out string path)
    {
        path = string.Empty;
        if (action == null || bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            return false;
        }

        InputBinding binding = action.bindings[bindingIndex];
        if (!IsBindingInControlScheme(binding, controlSchemeName))
        {
            return false;
        }

        path = NormalizeBindingPath(binding.effectivePath);
        return !string.IsNullOrEmpty(path);
    }

    private static bool IsMatchingCompositePart(InputBinding binding, string compositePartName, string controlSchemeName)
    {
        return !string.IsNullOrWhiteSpace(compositePartName)
            && binding.isPartOfComposite
            && IsBindingInControlScheme(binding, controlSchemeName)
            && string.Equals(binding.name, compositePartName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBindingInControlScheme(InputBinding binding, string controlSchemeName)
    {
        return IsGamepadControlScheme(controlSchemeName)
            ? IsGamepadBinding(binding)
            : IsKeyboardMouseBinding(binding);
    }

    private static bool IsKeyboardMouseBinding(InputBinding binding)
    {
        return ContainsKeyboardMouseGroup(binding.groups)
            || StartsWithDevice(binding.effectivePath, "<Keyboard>")
            || StartsWithDevice(binding.effectivePath, "<Mouse>");
    }

    private static bool IsGamepadBinding(InputBinding binding)
    {
        return ContainsGroup(binding.groups, GamepadGroup)
            || StartsWithDevice(binding.effectivePath, "<Gamepad>");
    }

    private static bool ContainsKeyboardMouseGroup(string groups)
    {
        return ContainsGroup(groups, KeyboardMouseGroup);
    }

    private static bool ContainsGroup(string groups, string groupName)
    {
        return !string.IsNullOrWhiteSpace(groups)
            && groups.IndexOf(groupName, StringComparison.OrdinalIgnoreCase) >= 0;
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

        string cleaned = NormalizeKnownGamepadDisplay(displayString.Trim());
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
            .Replace("enter", "Enter")
            .Replace("buttonSouth", "A")
            .Replace("buttonEast", "B")
            .Replace("buttonWest", "X")
            .Replace("buttonNorth", "Y")
            .Replace("leftStickPress", "L3")
            .Replace("rightStickPress", "R3")
            .Replace("Left Stick Press", "L3")
            .Replace("Right Stick Press", "R3")
            .Replace("Button South", "A")
            .Replace("Button East", "B")
            .Replace("Button West", "X")
            .Replace("Button North", "Y");
    }

    private static string NormalizeKnownGamepadDisplay(string displayString)
    {
        return displayString
            .Replace("<Gamepad>/dpad/up", "D-Pad Up")
            .Replace("<Gamepad>/dpad/down", "D-Pad Down")
            .Replace("<Gamepad>/dpad/left", "D-Pad Left")
            .Replace("<Gamepad>/dpad/right", "D-Pad Right")
            .Replace("<Gamepad>/rightTrigger", "RT")
            .Replace("<Gamepad>/leftTrigger", "LT")
            .Replace("<Gamepad>/rightShoulder", "RB")
            .Replace("<Gamepad>/leftShoulder", "LB")
            .Replace("<Gamepad>/startButton", "Start")
            .Replace("<Gamepad>/selectButton", "Select")
            .Replace("<Gamepad>/rightStickPress", "R3")
            .Replace("<Gamepad>/leftStickPress", "L3")
            .Replace("Right Trigger", "RT")
            .Replace("Left Trigger", "LT")
            .Replace("Right Shoulder", "RB")
            .Replace("Left Shoulder", "LB")
            .Replace("Start Button", "Start")
            .Replace("Select Button", "Select");
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
        public BindingDefinition(
            string displayName,
            string actionMapName,
            string actionName,
            string preferredPath,
            string compositePartName = null,
            string controlSchemeName = KeyboardMouseGroup)
        {
            DisplayName = displayName;
            ActionMapName = actionMapName;
            ActionName = actionName;
            PreferredPath = preferredPath;
            CompositePartName = compositePartName;
            ControlSchemeName = controlSchemeName;
        }

        public string DisplayName { get; }
        public string ActionMapName { get; }
        public string ActionName { get; }
        public string PreferredPath { get; }
        public string CompositePartName { get; }
        public string ControlSchemeName { get; }
    }
}
