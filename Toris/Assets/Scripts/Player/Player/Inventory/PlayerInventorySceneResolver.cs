using System;
using OutlandHaven.UIToolkit;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    internal static class PlayerInventorySceneResolver
    {
        private const string GameSessionResourcePath = "GameData/GameSession";
        private const string PlayerStatsAnchorResourcePath = "PlayerProgression/PlayerStatsAnchor";

        public static InventoryManager ResolvePlayerInventory(Component context, InventoryManager current)
        {
            if (IsPlayerInventory(current))
                return current;

            if (context != null)
            {
                InventoryManager parentInventory = context.GetComponentInParent<InventoryManager>();
                if (IsPlayerInventory(parentInventory))
                    return parentInventory;
            }

            GameSessionSO gameSession = LoadGameSession();
            if (gameSession != null && IsPlayerInventory(gameSession.PlayerInventory))
                return gameSession.PlayerInventory;

            InventoryManager[] inventoryManagers = UnityEngine.Object.FindObjectsByType<InventoryManager>(FindObjectsSortMode.None);
            for (int i = 0; i < inventoryManagers.Length; i++)
            {
                InventoryManager candidate = inventoryManagers[i];
                if (IsPlayerInventory(candidate))
                    return candidate;
            }

            return null;
        }

        public static InventoryManager ResolveEquipmentInventory(Component context, InventoryManager current, InventoryManager playerInventory)
        {
            if (IsEquipmentInventory(current, playerInventory))
                return current;

            InventoryManager[] inventoryManagers = UnityEngine.Object.FindObjectsByType<InventoryManager>(FindObjectsSortMode.None);
            InventoryManager fallbackInventory = null;

            for (int i = 0; i < inventoryManagers.Length; i++)
            {
                InventoryManager candidate = inventoryManagers[i];
                if (candidate == null || candidate == playerInventory)
                    continue;

                if (LooksLikeEquipmentInventory(candidate))
                    return candidate;

                if (fallbackInventory == null)
                    fallbackInventory = candidate;
            }

            return fallbackInventory;
        }

        public static InteractionPromptUI ResolveInteractionPrompt(InteractionPromptUI current)
        {
            if (current != null)
                return current;

            return UnityEngine.Object.FindFirstObjectByType<InteractionPromptUI>();
        }

        public static PlayerStatsAnchorSO ResolvePlayerStatsAnchor(PlayerStatsAnchorSO current)
        {
            if (current != null)
                return current;

            return Resources.Load<PlayerStatsAnchorSO>(PlayerStatsAnchorResourcePath);
        }

        private static GameSessionSO LoadGameSession()
        {
            return Resources.Load<GameSessionSO>(GameSessionResourcePath);
        }

        private static bool IsPlayerInventory(InventoryManager inventoryManager)
        {
            return inventoryManager != null
                   && inventoryManager.ContainerBlueprint != null
                   && inventoryManager.ContainerBlueprint.AssociatedView == ScreenType.Inventory;
        }

        private static bool IsEquipmentInventory(InventoryManager inventoryManager, InventoryManager playerInventory)
        {
            return inventoryManager != null
                   && inventoryManager != playerInventory
                   && LooksLikeEquipmentInventory(inventoryManager);
        }

        private static bool LooksLikeEquipmentInventory(InventoryManager inventoryManager)
        {
            if (inventoryManager == null)
                return false;

            string objectName = inventoryManager.gameObject.name;
            return !string.IsNullOrEmpty(objectName)
                   && objectName.IndexOf("Equip", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
