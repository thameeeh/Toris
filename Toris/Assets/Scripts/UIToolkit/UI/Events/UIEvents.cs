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
        SkillTree,
        PauseMenu,
        Vendor
    }
    public static class UIEvents
    {
        public static Action<ScreenType, object> OnRequestOpen;

        public static Action<ScreenType> OnRequestClose;

        public static Action OnRequestCloseAll;

        public static Action<ScreenType> OnScreenOpen;
    }
}