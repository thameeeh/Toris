using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public enum ScreenType
    {
//----- Game Play -----------------
        None,
        HUD, // Always on display

        Inventory,
        Smith,
        Mage,
        Potions,
        SageUpgrade,

        PlayerEquipment,
        Skills,
        PauseMenu,
        DeathScreen,

//----- Main Menu -----------------
        MainMenu,
        SettingsModal,
        ConfirmationModal
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
