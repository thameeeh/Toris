using UnityEngine;
using OutlandHaven.UIToolkit;

public class ItemTestDebugger : MonoBehaviour
{
    public InventoryItemSO MyItemBlueprint;

    void Start()
    {
        // 1. Create the live item from the blueprint
        ItemInstance myWand = new ItemInstance(MyItemBlueprint);

        // 2. Read static data (Always the same)
        var weaponStats = myWand.BaseItem.GetComponent<EquipableComponent>();
        if (weaponStats != null)
        {
            Debug.Log($"This weapon deals {weaponStats.StreangthBonus} damage.");
        }

        // 3. Read and modify dynamic data (Changes during gameplay)
        var charges = myWand.GetState<UpgradeableState>();
        if (charges != null)
        {
            Debug.Log($"Wand spawned with {charges.CurrentLevel} charges.");

            // Simulate using the item
            charges.CurrentLevel--;
            Debug.Log($"Wand used! It now has {charges.CurrentLevel} charges left.");
        }
    }
}