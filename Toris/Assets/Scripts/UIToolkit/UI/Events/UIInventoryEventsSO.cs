using UnityEngine;
using UnityEngine.Events;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UIInventoryEventsSO")]
    public class UIInventoryEventsSO : ScriptableObject
    {
        public UnityAction OnInventoryUpdated;

        [Header("Shop Events")]
        public UnityAction OnShopInventoryUpdated;
        public UnityAction<ItemInstance, int> OnRequestBuy;
        public UnityAction<ItemInstance, int> OnRequestSell;
        public UnityAction<int> OnCurrencyChanged;

        [Header("Crafting Events")]
        public UnityAction<InventorySlot> OnItemClicked;
        public UnityAction<InventorySlot, SalvageType> OnRequestSalvage;
        public UnityAction<InventorySlot, InventorySlot> OnRequestForge;

        [Header("Player Inventory Actions")]
        public UnityAction<InventorySlot> OnRequestEquip;
        public UnityAction<InventorySlot> OnRequestUse;
        public UnityAction<EquipmentSlot> OnRequestUnequip;
    }
}