using UnityEngine;
using OutlandHaven.Inventory;

public class InventoryActionController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private InventoryManager _inventory;
    [SerializeField] private PlayerEquipmentController _equipment;

    public bool TryEquipFromInventorySlot(int slotIndex)
    {
        if (_inventory == null || _equipment == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing inventory or equipment reference.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= _inventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Slot index {slotIndex} is out of range.");
            return false;
        }

        InventorySlot slot = _inventory.LiveSlots[slotIndex];
        if (slot == null || slot.IsEmpty || slot.HeldItem == null || slot.HeldItem.BaseItem == null)
        {
            Debug.LogWarning($"[InventoryActionController] Slot {slotIndex} is empty.");
            return false;
        }

        return _equipment.Equip(slot.HeldItem);
    }

    public bool TryEquipFromInventorySlot(InventorySlot slot)
    {
        if (_equipment == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing equipment reference.");
            return false;
        }

        if (slot == null || slot.IsEmpty || slot.HeldItem == null || slot.HeldItem.BaseItem == null)
        {
            Debug.LogWarning("[InventoryActionController] Cannot equip from a null or empty slot.");
            return false;
        }

        return _equipment.Equip(slot.HeldItem);
    }

    public bool TryUnequip(EquipmentSlot equipmentSlot)
    {
        if (_equipment == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing equipment reference.");
            return false;
        }

        return _equipment.Unequip(equipmentSlot);
    }

    public bool CanEquip(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || slot.HeldItem == null || slot.HeldItem.BaseItem == null)
            return false;

        return slot.HeldItem.BaseItem.GetComponent<EquipableComponent>() != null;
    }

    public bool CanUse(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || slot.HeldItem == null || slot.HeldItem.BaseItem == null)
            return false;

        return slot.HeldItem.BaseItem.GetComponent<ConsumableComponent>() != null;
    }
}