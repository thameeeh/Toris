using UnityEngine;

namespace OutlandHaven.Inventory
{
    public interface IUsable
    {
        bool TryUse(PlayerConsumableController consumableController, InventoryManager inventoryManager, InventorySlot slot);
    }
}
