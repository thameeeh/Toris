using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;

public class PlayerEquipmentController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The InventoryManager configured to act as the equipment container (e.g. 5 slots).")]
    [SerializeField] private InventoryManager _equipmentInventory;
    [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

    private readonly Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new();

    public event Action<EquipmentSlot, ItemInstance> OnItemEquipped;
    public event Action<EquipmentSlot, ItemInstance> OnItemUnequipped;
    public event Action<EquipmentSlot, ItemInstance> OnEquippedItemChanged;

    private void OnEnable()
    {
        if (_uiInventoryEvents != null)
        {
            _uiInventoryEvents.OnInventoryUpdated += RefreshEquipmentState;
        }

        RefreshEquipmentState();
    }

    private void OnDisable()
    {
        if (_uiInventoryEvents != null)
        {
            _uiInventoryEvents.OnInventoryUpdated -= RefreshEquipmentState;
        }
    }

    private void Start()
    {
        RefreshEquipmentState();
    }

    public void RefreshEquipmentState()
    {
        if (_equipmentInventory == null || _equipmentInventory.LiveSlots == null)
            return;

        // Hardcoded equipment inventory layout:
        // 0 = Head, 1 = Chest, 2 = Legs, 3 = Arms, 4 = Weapon
        ProcessSlot(0, EquipmentSlot.Head);
        ProcessSlot(1, EquipmentSlot.Chest);
        ProcessSlot(2, EquipmentSlot.Legs);
        ProcessSlot(3, EquipmentSlot.Arms);
        ProcessSlot(4, EquipmentSlot.Weapon);
    }

    private void ProcessSlot(int index, EquipmentSlot slotType)
    {
        if (index >= _equipmentInventory.LiveSlots.Count)
            return;

        InventorySlot slotData = _equipmentInventory.LiveSlots[index];
        ItemInstance currentItemInSlot = slotData.IsEmpty ? null : slotData.HeldItem;

        _equippedItems.TryGetValue(slotType, out ItemInstance previouslyEquippedItem);

        if (currentItemInSlot == previouslyEquippedItem)
            return;

        if (previouslyEquippedItem != null)
        {
            OnItemUnequipped?.Invoke(slotType, previouslyEquippedItem);
            _equippedItems.Remove(slotType);
        }

        if (currentItemInSlot != null)
        {
            _equippedItems[slotType] = currentItemInSlot;
            OnItemEquipped?.Invoke(slotType, currentItemInSlot);
        }
        else
        {
            _equippedItems.Remove(slotType);
        }

        OnEquippedItemChanged?.Invoke(slotType, currentItemInSlot);
    }

    public ItemInstance GetEquippedItem(EquipmentSlot slot)
    {
        _equippedItems.TryGetValue(slot, out ItemInstance item);
        return item;
    }

    public bool IsSlotOccupied(EquipmentSlot slot)
    {
        return _equippedItems.ContainsKey(slot);
    }

    public bool TryGetEquippedItem(EquipmentSlot slot, out ItemInstance item)
    {
        return _equippedItems.TryGetValue(slot, out item);
    }

    public IReadOnlyDictionary<EquipmentSlot, ItemInstance> GetAllEquippedItems()
    {
        return _equippedItems;
    }
}
