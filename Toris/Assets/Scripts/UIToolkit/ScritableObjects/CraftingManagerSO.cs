using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/CraftingManagerSO")]
    public class CraftingManagerSO : ScriptableObject
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
                InventoryEvents.OnRequestForge += HandleRequestForge;
            }
        }

        public void Cleanup()
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestForge -= HandleRequestForge;
            }
        }

        private void HandleRequestForge(InventorySlot slot1, InventorySlot slot2)
        {
            if (slot1 == null || slot1.IsEmpty || slot2 == null || slot2.IsEmpty) return;
            if (SessionData == null || SessionData.PlayerInventory == null || SessionData.PlayerData == null) return;
            if (Registry == null) return;

            // Cache items before doing anything that could invalidate references
            InventoryItemSO item1Type = slot1.HeldItem.BaseItem;
            InventoryItemSO item2Type = slot2.HeldItem.BaseItem;

            // Find a matching recipe
            CraftingRecipeSO recipe = GetMatchingRecipe(item1Type, item2Type);
            if (recipe == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Forge failed: No matching recipe found.");
#endif
                return;
            }

            // Check if player has enough gold
            if (SessionData.PlayerData.Gold < recipe.GoldCost)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Forge failed: Not enough gold. Need {recipe.GoldCost}, have {SessionData.PlayerData.Gold}.");
#endif
                return;
            }

            // Determine required quantities based on the recipe
            int slot1Req = 1;
            int slot2Req = 1;

            if (recipe.BaseItemRequirement == item1Type)
            {
                var matReq = recipe.MaterialRequirements.Find(m => m.Material == item2Type);
                slot2Req = matReq.Quantity;
            }
            else
            {
                var matReq = recipe.MaterialRequirements.Find(m => m.Material == item1Type);
                slot1Req = matReq.Quantity;
            }

            // Attempt to remove inputs from player inventory
            bool removedSlot1 = SessionData.PlayerInventory.RemoveItem(new ItemInstance(item1Type), slot1Req);
            if (removedSlot1)
            {
                bool removedSlot2 = SessionData.PlayerInventory.RemoveItem(new ItemInstance(item2Type), slot2Req);
                if (removedSlot2)
                {Debug.Log("Craft Packt has been made");
                    // Add output to player inventory
                    bool added = SessionData.PlayerInventory.AddItem(new ItemInstance(recipe.OutputItem), 1);
                    if (added)
                    {
                        
                        // Deduct gold
                        SessionData.PlayerData.ModifyGold(-recipe.GoldCost);
                        InventoryEvents?.OnCurrencyChanged?.Invoke(SessionData.PlayerData.Gold);
                        InventoryEvents?.OnInventoryUpdated?.Invoke();
#if UNITY_EDITOR
                        Debug.Log($"Forged {recipe.OutputItem.ItemName} successfully.");
#endif
                        return;
                    }
                    else
                    {
                        // Rollback slot 2 removal
                        SessionData.PlayerInventory.AddItem(new ItemInstance(item2Type), slot2Req);
#if UNITY_EDITOR
                        Debug.LogWarning("Forge failed: Inventory full. Refunded ingredients.");
#endif
                    }
                }

                // Rollback slot 1 removal
                SessionData.PlayerInventory.AddItem(new ItemInstance(item1Type), slot1Req);
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("Forge failed: Not enough items.");
#endif
            }
        }

        private CraftingRecipeSO GetMatchingRecipe(InventoryItemSO itemA, InventoryItemSO itemB)
        {
            foreach (var recipe in Registry.CraftingRecipes)
            {
                if (recipe == null) continue;

                // Check if itemA is the base and itemB is the material
                if (recipe.BaseItemRequirement == itemA &&
                    recipe.MaterialRequirements.Exists(m => m.Material == itemB))
                {
                    return recipe;
                }

                // Check if itemB is the base and itemA is the material
                if (recipe.BaseItemRequirement == itemB &&
                    recipe.MaterialRequirements.Exists(m => m.Material == itemA))
                {
                    return recipe;
                }
            }
            return null;
        }
    }
}
