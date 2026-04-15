using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/ShopManagerSO")]
    public class ShopManagerSO : ScriptableObject
    {
        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public PlayerProgressionAnchorSO PlayerAnchor;
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
            if (SessionData == null || SessionData.PlayerInventory == null) return;
            if (PlayerAnchor == null || !PlayerAnchor.IsReady) return;
            if (CurrentShopInventory == null) return;

            // Clamp quantity to how much stock the shop actually has to prevent bulk buy failures
            int shopStock = 0;
            foreach (var slot in CurrentShopInventory.LiveSlots)
            {
                if (slot != null && !slot.IsEmpty && slot.HeldItem.IsStackableWith(item))
                {
                    shopStock += slot.Count;
                }
            }

            quantity = Mathf.Min(quantity, shopStock);

            if (quantity <= 0) return; // Shop is out of stock

            // Clamp quantity to how much gold the player has
            int affordableQuantity = item.BaseItem.GoldValue > 0
                ? PlayerAnchor.Instance.CurrentGold / item.BaseItem.GoldValue
                : quantity; // If free, player can afford whatever the shop has

            quantity = Mathf.Min(quantity, affordableQuantity);

            if (quantity <= 0)
            {
#if UNITY_EDITOR
                Debug.LogWarning("Not enough gold to buy even one item.");
#endif
                return;
            }

            int totalCost = item.BaseItem.GoldValue * quantity;

            // Check if player has enough gold (redundant due to clamping, but safe)
            if (PlayerAnchor.Instance.CurrentGold >= totalCost)
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
                        PlayerAnchor.Instance.TrySpendGold(totalCost);

                        InventoryEvents?.OnShopInventoryUpdated?.Invoke();

#if UNITY_EDITOR
                        Debug.Log($"Bought {quantity} {item.BaseItem.ItemName} for {totalCost} gold. Remaining Gold: {PlayerAnchor.Instance.CurrentGold}");
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
        }

        private void HandleRequestSell(ItemInstance item, int quantity)
        {
            if (item == null || item.BaseItem == null || quantity <= 0) return;
            if (SessionData == null || SessionData.PlayerInventory == null) return;
            if (PlayerAnchor == null || !PlayerAnchor.IsReady) return;
            if (CurrentShopInventory == null) return;

            int totalValue = item.BaseItem.GoldValue * quantity;

            bool removed = SessionData.PlayerInventory.RemoveItem(item, quantity);

            if (removed)
            {
                // Add the item to the shop's inventory so the NPC can resell it
                bool addedToShop = CurrentShopInventory.AddItem(item, quantity);

                PlayerAnchor.Instance.AddGold(totalValue);
                InventoryEvents?.OnInventoryUpdated?.Invoke();

                if (addedToShop)
                {
                    InventoryEvents?.OnShopInventoryUpdated?.Invoke();
                }

#if UNITY_EDITOR
                Debug.Log($"Sold {quantity} {item.BaseItem.ItemName} for {totalValue} gold. Total Gold: {PlayerAnchor.Instance.CurrentGold}");
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
