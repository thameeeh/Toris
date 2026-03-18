using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/ShopManagerSO")]
    public class ShopManagerSO : ScriptableObject
    {
        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public UIInventoryEventsSO InventoryEvents;
        public UIEventsSO UIEvents; // Dependency for global UI events like screen open

        [System.NonSerialized]
        public InventoryManager CurrentShopInventory;

        public void Initialize()
        {
            Cleanup();
            Debug.Log("Hello from shop manager!");
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestBuy += HandleRequestBuy;
                InventoryEvents.OnRequestSell += HandleRequestSell;
            }
            if (UIEvents != null)
            {
                UIEvents.OnRequestOpen += HandleRequestOpen;
            }
        }

        public void Cleanup() 
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestBuy -= HandleRequestBuy;
                InventoryEvents.OnRequestSell -= HandleRequestSell;
            }
            if (UIEvents != null)
            {
                UIEvents.OnRequestOpen -= HandleRequestOpen;
            }
        }

        private void HandleRequestOpen(ScreenType screenType, object payload)
        {
            if (screenType == ScreenType.Smith || screenType == ScreenType.Mage) // Add any other vendor types here as they are created
            {
                if (payload is InventoryManager dynamicShopContainer)
                {
                    CurrentShopInventory = dynamicShopContainer;
#if UNITY_EDITOR
                    Debug.Log($"ShopManager updated CurrentShopInventory for {screenType}");
#endif
                }
            }
        }

        private void HandleRequestBuy(ItemInstance item, int quantity)
        {
            if (item == null || item.BaseItem == null || quantity <= 0) return;
            if (SessionData == null || SessionData.PlayerInventory == null || SessionData.PlayerData == null) return;
            if (CurrentShopInventory == null) return;

            int totalCost = item.BaseItem.GoldValue * quantity;

            // Check if player has enough gold
            if (SessionData.PlayerData.Gold >= totalCost)
            {
                // First check if the shop actually has enough items to sell
                bool removedFromShop = CurrentShopInventory.RemoveItem(item, quantity);

                if (removedFromShop)
                {
                    // Attempt to add to player inventory
                    bool added = SessionData.PlayerInventory.AddItem(item, quantity);

                    if (added)
                    {
                        // Deduct gold
                        SessionData.PlayerData.ModifyGold(-totalCost);

                        InventoryEvents?.OnCurrencyChanged?.Invoke(SessionData.PlayerData.Gold);
                        InventoryEvents?.OnShopInventoryUpdated?.Invoke();

#if UNITY_EDITOR
                        Debug.Log($"Bought {quantity} {item.BaseItem.ItemName} for {totalCost} gold. Remaining Gold: {SessionData.PlayerData.Gold}");
#endif
                    }
                    else
                    {
                        // If player inventory was full, refund the item to the shop
                        CurrentShopInventory.AddItem(item, quantity);
#if UNITY_EDITOR
                        Debug.LogWarning("Inventory full! Could not buy item. Refunded to shop.");
#endif
                    }
                }
                else
                {
#if UNITY_EDITOR
                    Debug.LogWarning("Shop does not have enough stock of the requested item.");
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

        private void HandleRequestSell(ItemInstance item, int quantity)
        {
            if (item == null || item.BaseItem == null || quantity <= 0) return;
            if (SessionData == null || SessionData.PlayerInventory == null || SessionData.PlayerData == null) return;
            if (CurrentShopInventory == null) return;

            int totalValue = item.BaseItem.GoldValue * quantity;

            bool removed = SessionData.PlayerInventory.RemoveItem(item, quantity);

            if (removed)
            {
                // Add the item to the shop's inventory so the NPC can resell it
                bool addedToShop = CurrentShopInventory.AddItem(item, quantity);

                SessionData.PlayerData.ModifyGold(totalValue);
                InventoryEvents?.OnCurrencyChanged?.Invoke(SessionData.PlayerData.Gold);
                InventoryEvents?.OnInventoryUpdated?.Invoke();

                if (addedToShop)
                {
                    InventoryEvents?.OnShopInventoryUpdated?.Invoke();
                }

#if UNITY_EDITOR
                Debug.Log($"Sold {quantity} {item.BaseItem.ItemName} for {totalValue} gold. Total Gold: {SessionData.PlayerData.Gold}");
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
