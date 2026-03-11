using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public enum SalvageType
    {
        Gold,
        Material
    }

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/SalvageManagerSO")]
    public class SalvageManagerSO : ScriptableObject
    {
        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public UIInventoryEventsSO InventoryEvents;
        public CraftingRegistrySO Registry;

        public void Initialize()
        {
            Cleanup();
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestSalvage += HandleRequestSalvage;
            }
        }

        public void Cleanup()
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestSalvage -= HandleRequestSalvage;
            }
        }

        private void HandleRequestSalvage(InventorySlot slot, SalvageType salvageType)
        {
            if (slot == null || slot.IsEmpty) return;
            if (SessionData == null || SessionData.PlayerInventory == null || SessionData.PlayerData == null) return;
            if (Registry == null) return;

            // Cache item type for safety
            InventoryItemSO itemType = slot.HeldItem.BaseItem;

            SalvageRecipeSO recipe = Registry.GetSalvageRecipeFor(itemType);
            if (recipe == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Salvage failed: No recipe found for {itemType.ItemName}.");
#endif
                return;
            }

            // Verify if player wants material but the recipe has no material yield
            if (salvageType == SalvageType.Material && recipe.MaterialYields.Count == 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Salvage failed: Recipe yields no material.");
#endif
                return;
            }

            bool removed = SessionData.PlayerInventory.RemoveItem(new ItemInstance(itemType), 1);
            if (!removed)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Salvage failed: Could not remove item from inventory.");
#endif
                return;
            }

            if (salvageType == SalvageType.Gold)
            {
                if (recipe.GoldYield > 0)
                {
                    SessionData.PlayerData.ModifyGold(recipe.GoldYield);
                    InventoryEvents?.OnCurrencyChanged?.Invoke(SessionData.PlayerData.Gold);
                }
#if UNITY_EDITOR
                Debug.Log($"Salvaged {itemType.ItemName} for {recipe.GoldYield} gold.");
#endif
            }
            else if (salvageType == SalvageType.Material)
            {
                // Give material rewards
                foreach (var yield in recipe.MaterialYields)
                {
                    bool added = SessionData.PlayerInventory.AddItem(new ItemInstance(yield.Material), yield.Quantity);
                    if (!added)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"Salvage failed to yield {yield.Material.ItemName}: Inventory full.");
#endif
                        // If we fail here, we could refund the salvaged item or handle the overflow, but we'll log it for now
                    }
                }
#if UNITY_EDITOR
                Debug.Log($"Salvaged {itemType.ItemName} for materials.");
#endif
            }

            InventoryEvents?.OnInventoryUpdated?.Invoke();
        }
    }
}
