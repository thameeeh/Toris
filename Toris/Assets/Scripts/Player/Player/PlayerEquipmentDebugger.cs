using UnityEngine;
using OutlandHaven.Inventory;

public class PlayerEquipmentDebugger : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentController _equipmentController;

    private void Reset()
    {
        if (_equipmentController == null)
            _equipmentController = GetComponent<PlayerEquipmentController>();
    }

    private void OnEnable()
    {
        if (_equipmentController == null)
            return;

        _equipmentController.OnItemEquipped += HandleItemEquipped;
        _equipmentController.OnItemUnequipped += HandleItemUnequipped;
        _equipmentController.OnEquippedItemChanged += HandleEquippedItemChanged;
    }

    private void OnDisable()
    {
        if (_equipmentController == null)
            return;

        _equipmentController.OnItemEquipped -= HandleItemEquipped;
        _equipmentController.OnItemUnequipped -= HandleItemUnequipped;
        _equipmentController.OnEquippedItemChanged -= HandleEquippedItemChanged;
    }

    private void HandleItemEquipped(EquipmentSlot slot, ItemInstance item)
    {
        Debug.Log($"[EquipmentDebugger] Equipped '{item?.BaseItem?.ItemName}' in slot {slot}.");
    }

    private void HandleItemUnequipped(EquipmentSlot slot, ItemInstance item)
    {
        Debug.Log($"[EquipmentDebugger] Unequipped '{item?.BaseItem?.ItemName}' from slot {slot}.");
    }

    private void HandleEquippedItemChanged(EquipmentSlot slot, ItemInstance item)
    {
        Debug.Log($"[EquipmentDebugger] Slot {slot} now has '{item?.BaseItem?.ItemName ?? "Nothing"}'.");
    }
}