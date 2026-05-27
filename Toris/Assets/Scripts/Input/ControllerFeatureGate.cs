using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public static class ControllerFeatureGate
{
    private const string GamepadGroup = "Gamepad";
    private const string JoystickGroup = "Joystick";
    private const string XrGroup = "XR";
    private const string GamepadPathPrefix = "<Gamepad>";
    private const string JoystickPathPrefix = "<Joystick>";
    private const string XrControllerPathPrefix = "<XRController>";
    private const string SharedSubmitPath = "*/{Submit}";
    private const string SharedCancelPath = "*/{Cancel}";
    private const string KeyboardSubmitPath = "<Keyboard>/enter";
    private const string KeyboardCancelPath = "<Keyboard>/escape";

#if TORIS_ENABLE_CONTROLLER_SUPPORT
    public const bool IsEnabled = true;
#else
    public const bool IsEnabled = false;
#endif

    public static void ApplyAvailability(InputSystem_Actions actions)
    {
        ApplyAvailability(actions?.asset);
    }

    public static void ApplyAvailability(InputActionAsset actionsAsset)
    {
        if (IsEnabled || actionsAsset == null)
        {
            return;
        }

        List<InputActionMap> enabledMaps = new List<InputActionMap>();
        foreach (InputActionMap actionMap in actionsAsset.actionMaps)
        {
            if (actionMap.enabled)
            {
                actionMap.Disable();
                enabledMaps.Add(actionMap);
            }
        }

        try
        {
            foreach (InputActionMap actionMap in actionsAsset.actionMaps)
            {
                foreach (InputAction action in actionMap.actions)
                {
                    for (int bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
                    {
                        InputBinding binding = action.bindings[bindingIndex];
                        if (TryGetKeyboardOnlyUiPath(binding, out string keyboardPath))
                        {
                            action.ApplyBindingOverride(bindingIndex, keyboardPath);
                        }
                        else if (IsControllerBinding(binding))
                        {
                            // Controller support stays dormant until the release define deliberately enables it.
                            action.ApplyBindingOverride(bindingIndex, string.Empty);
                        }
                    }
                }
            }
        }
        finally
        {
            for (int i = 0; i < enabledMaps.Count; i++)
            {
                enabledMaps[i].Enable();
            }
        }
    }

    private static bool TryGetKeyboardOnlyUiPath(InputBinding binding, out string keyboardPath)
    {
        keyboardPath = null;
        if (!ContainsGroup(binding.groups, GamepadGroup))
        {
            return false;
        }

        if (string.Equals(binding.path, SharedSubmitPath, StringComparison.Ordinal))
        {
            keyboardPath = KeyboardSubmitPath;
            return true;
        }

        if (string.Equals(binding.path, SharedCancelPath, StringComparison.Ordinal))
        {
            keyboardPath = KeyboardCancelPath;
            return true;
        }

        return false;
    }

    private static bool IsControllerBinding(InputBinding binding)
    {
        return ContainsGroup(binding.groups, GamepadGroup)
            || ContainsGroup(binding.groups, JoystickGroup)
            || ContainsGroup(binding.groups, XrGroup)
            || (!string.IsNullOrWhiteSpace(binding.path)
                && (binding.path.StartsWith(GamepadPathPrefix, StringComparison.OrdinalIgnoreCase)
                    || binding.path.StartsWith(JoystickPathPrefix, StringComparison.OrdinalIgnoreCase)
                    || binding.path.StartsWith(XrControllerPathPrefix, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool ContainsGroup(string groups, string expectedGroup)
    {
        if (!string.IsNullOrWhiteSpace(groups))
        {
            string[] bindingGroups = groups.Split(';');
            for (int i = 0; i < bindingGroups.Length; i++)
            {
                if (string.Equals(bindingGroups[i], expectedGroup, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
