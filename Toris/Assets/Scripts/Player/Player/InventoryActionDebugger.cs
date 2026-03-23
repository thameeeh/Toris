using UnityEngine;
using OutlandHaven.Inventory;

public class InventoryActionControllerDebugger : MonoBehaviour
{
    [SerializeField] private InventoryActionController _actions;
    [SerializeField] private int _slotIndex = 0;
    [SerializeField] private EquipmentSlot _unequipSlot = EquipmentSlot.Weapon;

    [ContextMenu("Try Equip From Inventory Slot")]
    public void TryEquipFromInventorySlot()
    {
        if (_actions == null)
        {
            Debug.LogWarning("[InventoryActionControllerDebugger] Missing action controller reference.");
            return;
        }

        bool result = _actions.TryEquipFromInventorySlot(_slotIndex);
        Debug.Log($"[InventoryActionControllerDebugger] TryEquipFromInventorySlot({_slotIndex}) => {result}");
    }

    [ContextMenu("Try Unequip Slot")]
    public void TryUnequipSlot()
    {
        if (_actions == null)
        {
            Debug.LogWarning("[InventoryActionControllerDebugger] Missing action controller reference.");
            return;
        }

        bool result = _actions.TryUnequip(_unequipSlot);
        Debug.Log($"[InventoryActionControllerDebugger] TryUnequip({_unequipSlot}) => {result}");
    }
}