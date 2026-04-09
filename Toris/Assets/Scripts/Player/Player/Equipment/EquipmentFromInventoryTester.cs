using UnityEngine;
using OutlandHaven.Inventory;

public class EquipmentFromInventoryTester : MonoBehaviour
{
    [SerializeField] private InventoryManager _inventory;
    [SerializeField] private PlayerEquipmentController _equipment;
    [SerializeField] private int _testSlotIndex = 0;

    [ContextMenu("Equip From Test Slot")]
    public void EquipFromTestSlot()
    {
        if (_inventory == null || _equipment == null)
        {
            Debug.LogWarning("[EquipmentFromInventoryTester] Missing references.");
            return;
        }

        if (_testSlotIndex < 0 || _testSlotIndex >= _inventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[EquipmentFromInventoryTester] Slot index {_testSlotIndex} is out of range.");
            return;
        }

        InventorySlot slot = _inventory.LiveSlots[_testSlotIndex];
        if (slot == null || slot.IsEmpty)
        {
            Debug.LogWarning($"[EquipmentFromInventoryTester] Slot {_testSlotIndex} is empty.");
            return;
        }

        // 1. Get the EquipableComponent from the item's blueprint
        EquipableComponent equipable = slot.HeldItem.BaseItem.GetComponent<EquipableComponent>();

        if (equipable == null)
        {
            Debug.LogWarning($"[EquipmentFromInventoryTester] Item in slot {_testSlotIndex} is not an equippable item.");
            return;
        }

        // 2. Extract the target EquipmentSlot enum
        EquipmentSlot targetSlot = equipable.TargetSlot;

        // 3. Pass it into TryGetEquippedItem
        bool equipped = _equipment.TryGetEquippedItem(targetSlot, out ItemInstance currentlyEquippedItem);

        // Optional: Check if the currently equipped item is the exact SAME item instance we are looking at
        bool isSameItem = equipped && (currentlyEquippedItem == slot.HeldItem);

        Debug.Log($"[EquipmentFromInventoryTester] Slot {targetSlot} occupied: {equipped}. Is it this exact item? {isSameItem}");
    }

    [ContextMenu("Log Inventory Slots")]
    public void LogInventorySlots()
    {
        if (_inventory == null)
        {
            Debug.LogWarning("[EquipmentFromInventoryTester] Missing inventory reference.");
            return;
        }

        for (int i = 0; i < _inventory.LiveSlots.Count; i++)
        {
            InventorySlot slot = _inventory.LiveSlots[i];

            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"Slot {i}: Empty");
            }
            else
            {
                Debug.Log($"Slot {i}: {slot.HeldItem.BaseItem.ItemName} x{slot.Count}");
            }
        }
    }
}
