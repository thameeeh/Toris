using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    // --- THE BLUEPRINT (Static Rules) ---
    [Serializable]
    public class DefensiveComponent : ItemComponent
    {
        [Tooltip("Flat reduction to physical damage.")]
        public float PhysicalDefense = 5f;

        [Tooltip("Flat reduction to magical/elemental damage.")]
        public float MagicalDefense = 0f;

        // No CreateInitialState() needed! Mitigation stats are static.
    }
}