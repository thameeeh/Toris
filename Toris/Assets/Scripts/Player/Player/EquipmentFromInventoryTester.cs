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

        bool equipped = _equipment.Equip(slot.HeldItem);
        Debug.Log($"[EquipmentFromInventoryTester] Equip from slot {_testSlotIndex}: {equipped}");
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