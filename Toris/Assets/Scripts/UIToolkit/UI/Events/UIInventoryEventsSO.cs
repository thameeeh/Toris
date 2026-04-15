using UnityEngine;
using UnityEngine.Events;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UIInventoryEventsSO")]
    public class UIInventoryEventsSO : ScriptableObject
    {
        public UnityAction OnInventoryUpdated;
        
        [Header("Targeted Updates")]
        public UnityAction<InventorySlot, InventorySlot> OnSpecificSlotsUpdated;

        [Header("Shop Events")]
        public UnityAction OnShopInventoryUpdated;
        public UnityAction<ItemInstance, int> OnRequestBuy;
        public UnityAction<ItemInstance, int> OnRequestSell;

        [Header("Crafting Events")]
        public UnityAction<InventorySlot> OnItemClicked;
        public UnityAction<InventorySlot> OnItemRightClicked;
        public UnityAction<InventorySlot, SalvageType> OnRequestSalvage;
        public UnityAction<InventorySlot, InventorySlot> OnRequestForge;

        [Header("Player Inventory Actions")]
        public UnityAction<InventorySlot> OnRequestEquip;
        public UnityAction<InventorySlot> OnRequestUse;
        public UnityAction<EquipmentSlot> OnRequestUnequip;
        
        [Header("Drag and Drop Events")]
        public UnityAction<InventoryManager, InventorySlot, InventoryManager, InventorySlot> OnRequestMoveItem;

        // Fired when an item is dropped onto a proxy visual slot (like Forge/Salvage)
        public UnityAction<InventorySlot, string> OnRequestSelectForProcessing;

        [Header("Context Management")]
        public UnityAction<InventoryInteractionContext> OnInteractionContextChanged;
    }
}