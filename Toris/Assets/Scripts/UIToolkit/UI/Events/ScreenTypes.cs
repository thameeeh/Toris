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
        Smith,
        Mage
    }
    public enum ScreenZone
    {
        HUD,
        Left,
        Right,
        Modal
    }
    public static class UIEvents
    {
        public static Action<ScreenType> OnScreenOpen;
    }
}