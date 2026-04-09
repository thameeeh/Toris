using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public enum ScreenType
    {
        None,
        HUD,
        Inventory,
        PauseMenu,
        CharacterSheet,
        Smith,
        Mage,
        SkillScreen
    }
    public enum ScreenZone
    {
        HUD,
        Left,
        Right,
        FullScreen,
        Modal
    }
    public static class UIEvents
    {
        public static Action<ScreenType> OnScreenOpen;
    }
}