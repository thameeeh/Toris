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
            // CRITICAL FIX: Added !inventoryManager.ContainerBlueprint.IsEquipment
            return inventoryManager != null
                   && inventoryManager.ContainerBlueprint != null
                   && inventoryManager.ContainerBlueprint.AssociatedView == ScreenType.Inventory
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

            // CRITICAL FIX: Check the Blueprint and Filters first, just like InventoryManager.cs does
            if (inventoryManager.ContainerBlueprint != null)
            {
                if (inventoryManager.ContainerBlueprint.IsEquipment) return true;

                if (inventoryManager.ContainerBlueprint.PredefinedFilters != null &&
                    inventoryManager.ContainerBlueprint.PredefinedFilters.Length > 0)
                {
                    SlotFilterType firstFilter = inventoryManager.ContainerBlueprint.PredefinedFilters[0];
                    if (firstFilter == SlotFilterType.Head ||
                        firstFilter == SlotFilterType.Chest ||
                        firstFilter == SlotFilterType.Legs ||
                        firstFilter == SlotFilterType.Arms ||
                        firstFilter == SlotFilterType.Weapon)
                    {
                        return true;
                    }
                }
            }

            // Fallback to name check
            string objectName = inventoryManager.gameObject.name;
            return !string.IsNullOrEmpty(objectName)
                   && objectName.IndexOf("Equip", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
