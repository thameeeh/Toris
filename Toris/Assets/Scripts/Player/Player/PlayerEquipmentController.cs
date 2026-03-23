using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

public class PlayerEquipmentController : MonoBehaviour
{
    private readonly Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new();

    public event Action<EquipmentSlot, ItemInstance> OnItemEquipped;
    public event Action<EquipmentSlot, ItemInstance> OnItemUnequipped;
    public event Action<EquipmentSlot, ItemInstance> OnEquippedItemChanged;

    public bool Equip(ItemInstance item)
    {
        if (item == null || item.BaseItem == null)
        {
            Debug.LogWarning("[PlayerEquipmentController] Cannot equip a null item.");
            return false;
        }

        EquipableComponent equipable = item.BaseItem.GetComponent<EquipableComponent>();
        if (equipable == null)
        {
            Debug.LogWarning($"[PlayerEquipmentController] Item '{item.BaseItem.ItemName}' is not equippable.");
            return false;
        }

        EquipmentSlot slot = equipable.TargetSlot;

        if (_equippedItems.TryGetValue(slot, out ItemInstance existingItem) && existingItem != null)
        {
            OnItemUnequipped?.Invoke(slot, existingItem);
        }

        _equippedItems[slot] = item;

        OnItemEquipped?.Invoke(slot, item);
        OnEquippedItemChanged?.Invoke(slot, item);

        return true;
    }

    public bool Unequip(EquipmentSlot slot)
    {
        if (!_equippedItems.TryGetValue(slot, out ItemInstance existingItem))
            return false;

        _equippedItems.Remove(slot);

        OnItemUnequipped?.Invoke(slot, existingItem);
        OnEquippedItemChanged?.Invoke(slot, null);

        return true;
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

    public void UnequipAll()
    {
        if (_equippedItems.Count == 0)
            return;

        EquipmentSlot[] slots = new EquipmentSlot[_equippedItems.Count];
        _equippedItems.Keys.CopyTo(slots, 0);

        for (int i = 0; i < slots.Length; i++)
        {
            Unequip(slots[i]);
        }
    }
}