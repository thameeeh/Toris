using System;
using OutlandHaven.UIToolkit;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    public static class PlayerInventorySceneResolver
    {
        private const string GameSessionResourcePath = "GameData/GameSession";
        private const string PlayerStatsAnchorResourcePath = "PlayerProgression/PlayerStatsAnchor";

        public static InventoryManager ResolvePlayerInventory(Component context, InventoryManager current)
        {
            // Priority 1: Use the instance registered in the Global Session (Source of Truth for UI)
            GameSessionSO gameSession = LoadGameSession();
            if (gameSession != null && IsPlayerInventory(gameSession.PlayerInventory))
            {
                if (current != null && current != gameSession.PlayerInventory)
                {
                    Debug.Log($"[Resolver] Syncing Player Inventory reference. Moving from '{current.name}' to session instance '{gameSession.PlayerInventory.name}'.");
                }
                return gameSession.PlayerInventory;
            }

            // Priority 2: Use current if it's already valid and we don't have a session override
            if (IsPlayerInventory(current))
                return current;

            // Priority 3: Search local context (e.g. if we are on the Player GameObject)
            if (context != null)
            {
                InventoryManager parentInventory = context.GetComponentInParent<InventoryManager>();
                if (IsPlayerInventory(parentInventory))
                    return parentInventory;
            }

            // Priority 4: Search whole scene as a fallback
            InventoryManager[] inventoryManagers = UnityEngine.Object.FindObjectsByType<InventoryManager>(FindObjectsSortMode.None);
            for (int i = 0; i < inventoryManagers.Length; i++)
            {
                InventoryManager candidate = inventoryManagers[i];
                if (IsPlayerInventory(candidate))
                    return candidate;
            }

            return null;
        }

        public static InventoryManager ResolvePotionInventory(InventoryManager current)
        {
            // Priority 1: Global Session
            GameSessionSO gameSession = LoadGameSession();
            if (gameSession != null && IsPotionInventory(gameSession.PlayerPotionInventory))
            {
                if (current != null && current != gameSession.PlayerPotionInventory)
                {
                    Debug.Log($"[Resolver] Syncing Potion reference. Moving from '{current.name}' to session instance '{gameSession.PlayerPotionInventory.name}'.");
                }
                return gameSession.PlayerPotionInventory;
            }

            // Priority 2: Current valid
            if (IsPotionInventory(current))
                return current;

            // Priority 3: Search whole scene
            InventoryManager[] inventoryManagers = UnityEngine.Object.FindObjectsByType<InventoryManager>(FindObjectsSortMode.None);
            for (int i = 0; i < inventoryManagers.Length; i++)
            {
                InventoryManager candidate = inventoryManagers[i];
                if (IsPotionInventory(candidate))
                    return candidate;
            }

            return null;
        }

        public static InventoryManager ResolveEquipmentInventory(Component context, InventoryManager current, InventoryManager playerInventory)
        {
            // Priority 1: Global Session
            GameSessionSO gameSession = LoadGameSession();
            if (gameSession != null && IsEquipmentInventory(gameSession.PlayerEquipment, playerInventory))
            {
                if (current != null && current != gameSession.PlayerEquipment)
                {
                    Debug.Log($"[Resolver] Syncing Equipment reference. Moving from '{current.name}' to session instance '{gameSession.PlayerEquipment.name}'.");
                }
                return gameSession.PlayerEquipment;
            }

            // Priority 2: Current valid
            if (IsEquipmentInventory(current, playerInventory))
                return current;

            // Priority 3: Search whole scene
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

        private static bool IsPotionInventory(InventoryManager inventoryManager)
        {
            return inventoryManager != null
                   && inventoryManager.ContainerBlueprint != null
                   && inventoryManager.ContainerBlueprint.AssociatedView == ScreenType.Potions;
        }

        private static bool IsPlayerInventory(InventoryManager inventoryManager)
        {
            if (inventoryManager == null || inventoryManager.ContainerBlueprint == null)
                return false;

            // Priority 1: Explicitly marked as backpack
            if (inventoryManager.ContainerBlueprint.IsBackpack)
                return true;

            // Priority 2: Matches the standard inventory view and is NOT equipment
            return inventoryManager.ContainerBlueprint.AssociatedView == ScreenType.Inventory
                   && !inventoryManager.ContainerBlueprint.IsEquipment;
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

            // Check blueprint first (most reliable)
            if (inventoryManager.ContainerBlueprint != null && inventoryManager.ContainerBlueprint.IsEquipment)
                return true;

            // Fallback to name-based check
            string objectName = inventoryManager.gameObject.name;
            return !string.IsNullOrEmpty(objectName)
                   && objectName.IndexOf("Equip", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
