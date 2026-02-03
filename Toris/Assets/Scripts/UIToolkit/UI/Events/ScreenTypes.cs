using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public enum ScreenType
    {
        None,
        HUD,
        Inventory,
        CharacterSheet,
        PauseMenu,
        Smith
    }
    public static class UIEvents
    {
        public static Action<ScreenType> OnScreenOpen;
    }
}