using OutlandHaven.Inventory;
using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public enum ProgressionCategory
    {
        Material,
        QuestItem,
        Key,
        Junk
    }

    // --- THE BLUEPRINT (Static Rules) ---
    [Serializable]
    public class ProgressionComponent : ItemComponent
    {
        [Tooltip("Helps the inventory sort or filter these items.")]
        public ProgressionCategory Category = ProgressionCategory.Material;

        // No CreateInitialState() needed! 
        // 50 Iron Ores are identical to 50 other Iron Ores, making them perfectly stackable.
    }
}