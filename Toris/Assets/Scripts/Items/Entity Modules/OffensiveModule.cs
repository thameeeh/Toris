using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    // --- THE BLUEPRINT (Static Rules) ---
    [Serializable]
    public class OffensiveComponent : ItemComponent
    {
        public float BaseDamage = 10f;

        [Tooltip("Attacks per second.")]
        public float AttackSpeed = 1.0f;

        // No CreateInitialState() needed! Base damage is static.
    }
}