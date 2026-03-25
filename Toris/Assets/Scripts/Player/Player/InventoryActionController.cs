using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;

public class InventoryActionController : MonoBehaviour
{
    [Header("Runtime References")]
    [SerializeField] private InventoryManager _playerInventory;
    [SerializeField] private InventoryManager _equipmentInventory;
    [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;

    private void OnEnable()
    {
        if (_uiInventoryEvents == null)
            return;

        _uiInventoryEvents.OnRequestEquip += HandleRequestEquip;
        _uiInventoryEvents.OnRequestUse += HandleRequestUse;
        _uiInventoryEvents.OnRequestUnequip += HandleRequestUnequip;
    }

    private void OnDisable()
    {
        if (_uiInventoryEvents == null)
            return;

        _uiInventoryEvents.OnRequestEquip -= HandleRequestEquip;
        _uiInventoryEvents.OnRequestUse -= HandleRequestUse;
        _uiInventoryEvents.OnRequestUnequip -= HandleRequestUnequip;
    }

    private void HandleRequestEquip(InventorySlot slot)
    {
        TryEquipFromInventorySlot(slot);
    }

    private void HandleRequestUse(InventorySlot slot)
    {
        if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null)
            return;

        Debug.Log($"[InventoryActionController] Use requested for '{slot.HeldItem.BaseItem.ItemName}', but consumable flow is not implemented yet.");
    }

    private void HandleRequestUnequip(EquipmentSlot slot)
    {
        TryUnequip(slot);
    }

    public bool TryEquipFromInventorySlot(int slotIndex)
    {
        if (_playerInventory == null || _equipmentInventory == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing player inventory or equipment inventory reference.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= _playerInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Slot index {slotIndex} is out of range.");
            return false;
        }

        return TryEquipFromInventorySlot(_playerInventory.LiveSlots[slotIndex]);
    }

    public bool TryEquipFromInventorySlot(InventorySlot sourceSlot)
    {
        if (_playerInventory == null || _equipmentInventory == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing player inventory or equipment inventory reference.");
            return false;
        }

        if (sourceSlot == null || sourceSlot.IsEmpty || sourceSlot.HeldItem == null || sourceSlot.HeldItem.BaseItem == null)
        {
            Debug.LogWarning("[InventoryActionController] Cannot equip from a null or empty slot.");
            return false;
        }

        EquipableComponent equipable = sourceSlot.HeldItem.BaseItem.GetComponent<EquipableComponent>();
        if (equipable == null)
        {
            Debug.LogWarning("[InventoryActionController] Item is not equippable.");
            return false;
        }

        int equipmentIndex = (int)equipable.TargetSlot;
        if (equipmentIndex < 0 || equipmentIndex >= _equipmentInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Equipment slot index {equipmentIndex} is out of range.");
            return false;
        }

        InventorySlot equipmentSlot = _equipmentInventory.LiveSlots[equipmentIndex];

        if (!equipmentSlot.IsEmpty && ReferenceEquals(equipmentSlot.HeldItem, sourceSlot.HeldItem))
            return true;

        if (equipmentSlot.IsEmpty)
        {
            equipmentSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
            sourceSlot.Clear();
        }
        else
        {
            ItemInstance tempItem = equipmentSlot.HeldItem;
            int tempCount = equipmentSlot.Count;

            equipmentSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
            sourceSlot.SetItem(tempItem, tempCount);
        }

        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
        return true;
    }

    public bool TryUnequip(EquipmentSlot equipmentSlotType)
    {
        if (_playerInventory == null || _equipmentInventory == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing player inventory or equipment inventory reference.");
            return false;
        }

        int equipmentIndex = (int)equipmentSlotType;
        if (equipmentIndex < 0 || equipmentIndex >= _equipmentInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Equipment slot index {equipmentIndex} is out of range.");
            return false;
        }

        InventorySlot equipmentSlot = _equipmentInventory.LiveSlots[equipmentIndex];
        if (equipmentSlot == null || equipmentSlot.IsEmpty || equipmentSlot.HeldItem == null)
        {
            Debug.LogWarning($"[InventoryActionController] Equipment slot {equipmentSlotType} is empty.");
            return false;
        }

        bool addedBack = _playerInventory.AddItem(equipmentSlot.HeldItem, equipmentSlot.Count);
        if (!addedBack)
        {
            Debug.LogWarning("[InventoryActionController] Could not unequip item because the player inventory has no space.");
            return false;
        }

        equipmentSlot.Clear();
        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
        return true;
    }

    public bool CanEquip(InventorySlot slot)
    {
        return slot != null &&
               !slot.IsEmpty &&
               slot.HeldItem?.BaseItem != null &&
               slot.HeldItem.BaseItem.GetComponent<EquipableComponent>() != null;
    }

    public bool CanUse(InventorySlot slot)
    {
        return slot != null &&
               !slot.IsEmpty &&
               slot.HeldItem?.BaseItem != null &&
               slot.HeldItem.BaseItem.GetComponent<ConsumableComponent>() != null;
    }
}