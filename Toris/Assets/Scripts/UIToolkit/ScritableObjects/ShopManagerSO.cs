using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/ShopManagerSO")]
    public class ShopManagerSO : ScriptableObject
    {
        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public UIInventoryEventsSO InventoryEvents;

        // This could be passed dynamically via the UI event payload when talking to different NPCs,
        // but for a standalone system a master or current shop container reference is typical.
        public InventoryContainerSO CurrentShopInventory;

        private void OnEnable()
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestBuy += HandleRequestBuy;
                InventoryEvents.OnRequestSell += HandleRequestSell;
            }
        }

        private void OnDisable()
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestBuy -= HandleRequestBuy;
                InventoryEvents.OnRequestSell -= HandleRequestSell;
            }
        }

        private void HandleRequestBuy(InventoryItemSO item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            if (SessionData == null || SessionData.PlayerInventory == null) return;
            if (CurrentShopInventory == null) return;

            int totalCost = item.GoldValue * quantity;

            // Check if player has enough gold
            if (SessionData.Gold >= totalCost)
            {
                // Attempt to add to player inventory
                bool added = SessionData.PlayerInventory.AddItem(item, quantity);

                if (added)
                {
                    // Deduct gold
                    SessionData.Gold -= totalCost;

                    // Note: Removing from ShopInventory could be done here if the shop has limited stock.
                    // For now, assuming infinite or handling via separate logic if shop also uses RemoveItem.

                    InventoryEvents?.OnCurrencyChanged?.Invoke(SessionData.Gold);

#if UNITY_EDITOR
                    Debug.Log($"Bought {quantity} {item.ItemName} for {totalCost} gold. Remaining Gold: {SessionData.Gold}");
#endif
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Inventory full! Could not buy item.");
#endif
                }
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("Not enough gold to buy item.");
#endif
            }
        }

        private void HandleRequestSell(InventoryItemSO item, int quantity)
        {
            if (item == null || quantity <= 0) return;
            if (SessionData == null || SessionData.PlayerInventory == null) return;
            // CurrentShopInventory could be used to receive the item if shop stock is dynamic.

            int totalValue = item.GoldValue * quantity;

            bool removed = SessionData.PlayerInventory.RemoveItem(item, quantity);

            if (removed)
            {
                SessionData.Gold += totalValue;
                InventoryEvents?.OnCurrencyChanged?.Invoke(SessionData.Gold);
                InventoryEvents?.OnInventoryUpdated?.Invoke();

#if UNITY_EDITOR
                Debug.Log($"Sold {quantity} {item.ItemName} for {totalValue} gold. Total Gold: {SessionData.Gold}");
#endif
            }
            else
            {
#if UNITY_EDITOR
                Debug.LogWarning("Could not sell item. Not found in inventory.");
#endif
            }
        }
    }
}
