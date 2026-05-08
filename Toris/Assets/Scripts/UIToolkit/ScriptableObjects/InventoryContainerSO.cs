using UnityEngine;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{
    [CreateAssetMenu(menuName = "UI/Inventory/Container Blueprint")]
    public class InventoryContainerSO : ScriptableObject
    {
        public int SlotCount = 20;
        public ScreenType AssociatedView = ScreenType.None;

        [Header("Optional. Predefined filters for specific slots by index.")]
        public SlotFilterType[] PredefinedFilters;
    }
}
