using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/CraftingManagerSO")]
    public class CraftingManagerSO : ScriptableObject
    {
        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public PlayerProgressionAnchorSO PlayerAnchor;
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

        public bool CanForge(CraftingRecipeSO recipe, InventorySlot slot1, InventorySlot slot2, out int slot1Req, out int slot2Req)
        {
            slot1Req = 0;
            slot2Req = 0;

            if (recipe == null || slot1 == null || slot1.IsEmpty || slot2 == null || slot2.IsEmpty) return false;

            InventoryItemSO item1Type = slot1.HeldItem.BaseItem;
            InventoryItemSO item2Type = slot2.HeldItem.BaseItem;

            // Determine required quantities based on the recipe before early returns
            slot1Req = 1;
            slot2Req = 1;

            if (recipe.BaseItemRequirement == item1Type)
            {
                var matReq = recipe.MaterialRequirements.Find(m => m.Material == item2Type);
                if (matReq.Material != null) slot2Req = matReq.Quantity;
            }
            else
            {
                var matReq = recipe.MaterialRequirements.Find(m => m.Material == item1Type);
                if (matReq.Material != null) slot1Req = matReq.Quantity;
            }

            if (SessionData == null || SessionData.PlayerInventory == null) return false;
            if (PlayerAnchor == null || !PlayerAnchor.IsReady) return false;

            // Check if player has enough gold
            bool hasGold = PlayerAnchor.Instance.CurrentGold >= recipe.GoldCost;
            if (!hasGold) return false;

            // Count total available for slot 1 item
            int totalItem1 = 0;
            foreach (var s in SessionData.PlayerInventory.LiveSlots)
            {
                if (!s.IsEmpty && s.HeldItem.IsStackableWith(new ItemInstance(item1Type)))
                    totalItem1 += s.Count;
            }

            // Count total available for slot 2 item
            int totalItem2 = 0;
            foreach (var s in SessionData.PlayerInventory.LiveSlots)
            {
                if (!s.IsEmpty && s.HeldItem.IsStackableWith(new ItemInstance(item2Type)))
                    totalItem2 += s.Count;
            }

            // If items are the same base type, we need enough for both combined
            if (item1Type == item2Type)
            {
                return totalItem1 >= (slot1Req + slot2Req);
            }
            else
            {
                return totalItem1 >= slot1Req && totalItem2 >= slot2Req;
            }
        }

        private void HandleRequestForge(InventorySlot slot1, InventorySlot slot2)
        {
            if (slot1 == null || slot1.IsEmpty || slot2 == null || slot2.IsEmpty) return;
            if (SessionData == null || SessionData.PlayerInventory == null) return;
            if (PlayerAnchor == null || !PlayerAnchor.IsReady) return;
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
            if (PlayerAnchor.Instance.CurrentGold < recipe.GoldCost)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Forge failed: Not enough gold. Need {recipe.GoldCost}, have {PlayerAnchor.Instance.CurrentGold}.");
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
                        PlayerAnchor.Instance.TrySpendGold(recipe.GoldCost);
                        InventoryEvents?.OnCurrencyChanged?.Invoke(PlayerAnchor.Instance.CurrentGold);
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

        public CraftingRecipeSO GetMatchingRecipe(InventoryItemSO itemA, InventoryItemSO itemB)
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
