using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Crafting/Upgrade and Salvage Manager")]
    public class UpgradeSalvageManagerSO : ScriptableObject
    {
        [Header("References")]
        public GameSessionSO SessionData;
        public CraftingRegistrySO Registry;

        [Header("Settings")]
        [Tooltip("Base gold cost multiplied by the item's current level")]
        public int UpgradeBaseGoldCost = 50;

        /// <summary>
        /// Calculates the gold cost to upgrade an item.
        /// For example: Cost = BaseCost * CurrentLevel
        /// </summary>
        public int CalculateUpgradeCost(ItemInstance itemInstance)
        {
            if (itemInstance == null) return 0;

            var upgradeState = itemInstance.GetState<UpgradeableState>();
            if (upgradeState != null)
            {
                return UpgradeBaseGoldCost * upgradeState.CurrentLevel;
            }
            return 0;
        }

        /// <summary>
        /// Attempts to upgrade an item in a specific slot, taking gold from the player.
        /// </summary>
        public bool TryUpgradeItem(InventorySlot slot)
        {
            if (slot == null || slot.IsEmpty)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Upgrade failed: Slot is empty.");
#endif
                return false;
            }

            if (SessionData == null || SessionData.PlayerData == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Upgrade failed: SessionData or PlayerData is missing.");
#endif
                return false;
            }

            var upgradeState = slot.HeldItem.GetState<UpgradeableState>();
            if (upgradeState == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Upgrade failed: Item does not have an UpgradeableState.");
#endif
                return false;
            }

            var upgradeableComponent = slot.HeldItem.BaseItem.GetComponent<UpgradeableComponent>();
            if (upgradeableComponent == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Upgrade failed: Item does not have an UpgradeableComponent.");
#endif
                return false;
            }

            if (upgradeState.CurrentLevel >= upgradeableComponent.MaxLevel)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Upgrade failed: Item is already at Max Level ({upgradeableComponent.MaxLevel}).");
#endif
                return false;
            }

            int cost = CalculateUpgradeCost(slot.HeldItem);

            if (SessionData.PlayerData.Gold >= cost)
            {
                // Deduct gold
                SessionData.PlayerData.ModifyGold(-cost);

                // Upgrade instance
                upgradeState.CurrentLevel++;
                slot.HeldItem.NotifyStateChanged(); // notification that item was upgraded and we need to update stats

#if UNITY_EDITOR
                Debug.Log($"Upgraded {slot.HeldItem.BaseItem.ItemName} to level {upgradeState.CurrentLevel} for {cost} gold.");
#endif
                // Events will be handled in a future task or fired by PlayerData
                return true;
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Upgrade failed: Not enough gold. Have {SessionData.PlayerData.Gold}, Need {cost}");
#endif
                return false;
            }
        }

        /// <summary>
        /// Attempts to salvage an item from a specific container, giving the player the yields.
        /// </summary>
        public bool TrySalvageItem(InventoryManager container, ItemInstance itemInstanceToSalvage)
        {
            if (container == null || itemInstanceToSalvage == null) return false;
            if (Registry == null)
            {
#if UNITY_EDITOR
                Debug.LogError("Salvage failed: Registry is missing.");
#endif
                return false;
            }

            SalvageRecipeSO recipe = Registry.GetSalvageRecipeFor(itemInstanceToSalvage.BaseItem);

            if (recipe == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Salvage failed: No salvage recipe found for {itemInstanceToSalvage.BaseItem.ItemName}.");
#endif
                return false;
            }

            // Attempt to remove exactly 1 instance of this item
            bool removed = container.RemoveItem(itemInstanceToSalvage, 1);
            if (!removed)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Salvage failed: Could not remove item from container.");
#endif
                return false;
            }

            // Give rewards
            if (recipe.GoldYield > 0 && SessionData != null && SessionData.PlayerData != null)
            {
                SessionData.PlayerData.ModifyGold(recipe.GoldYield);
            }

            // Skeleton for giving item rewards
            /*
            foreach (var yield in recipe.MaterialYields)
            {
                ItemInstance newMat = new ItemInstance(yield.Material);
                container.AddItem(newMat, yield.Quantity);
            }
            */

#if UNITY_EDITOR
            Debug.Log($"Salvaged {itemInstanceToSalvage.BaseItem.ItemName} for {recipe.GoldYield} gold.");
#endif

            return true;
        }
    }
}
