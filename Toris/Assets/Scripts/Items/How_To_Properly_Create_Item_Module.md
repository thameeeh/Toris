"""# Outland Haven: Item Component & State Creation Guide

**Team Toris Internal Documentation**
This guide outlines the strict ruleset for adding new modular behaviors to items in Outland Haven. Our architecture uses a **Blueprint/State pattern** to keep runtime memory low and ensure save files (via Newtonsoft.Json) remain stable.

---

## 1. The Core Philosophy
Every new item behavior requires asking one question: **"Does this data change during gameplay?"**
* **NO (Static Data):** It belongs in the `ItemComponent` (The Blueprint).
* **YES (Dynamic Data):** It belongs in the `ItemComponentState` (The Runtime DTO).

---

## 2. Rules for Building `ItemComponent` (The Blueprint)
The Component lives on the `InventoryItemSO`. It defines the rules and initial setup.

* **Rule 2.1: Unity References Go Here:** This is the ONLY place you can store references to `GameObjects`, `ParticleSystems`, `AudioClips`, or other `ScriptableObjects`.
* **Rule 2.2: Avoid Redundancy:** Do not duplicate data. If a sword has a `BaseDamage` of 10, it stays here. Do not copy it into the runtime State.
* **Rule 2.3: Generate the State:** If your module needs to track live data (like durability), you MUST override `CreateInitialState()`.
* **Rule 2.4: Purely Static Modules:** If your module is purely static (e.g., `OffensiveComponent` just granting base damage), you do *not* need to create a paired State class.

---

## 3. Rules for Building `ItemComponentState` (The Runtime)
The State is created at runtime and is directly serialized to the hard drive during a save. **Treat it as a pure Data Transfer Object (DTO).**

* **Rule 3.1: NO Unity Engine References:** A State cannot hold `GameObjects`, `Transforms`, or `ScriptableObjects`. Doing so will crash the JSON serializer.
* **Rule 3.2: The Parameterless Constructor (CRITICAL):** You MUST include a public, empty constructor (e.g., `public MyState() { }`). Newtonsoft.Json requires this to rebuild the object when loading a save file.
* **Rule 3.3: Value-Based Stacking:** When overriding `IsStackableWith()`, compare the *actual values*, not the object references. (e.g., `this.CurrentDurability == other.CurrentDurability`).
* **Rule 3.4: Deep Cloning:** You MUST override `Clone()` and copy over every variable. This is vital for splitting inventory stacks and transferring items between scenes safely.
* **Rule 3.5: No Heavy Logic:** Keep complex game loops out of the State. It should only hold data and simple mutator methods (like `AddDurability(int amount)`).

---

## 4. Cheat Sheet: Code Template

Use this template when creating a new module to ensure compliance.

Code output

File generated successfully.

```csharp
using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    // ==========================================
    // 1. THE BLUEPRINT (Static Rules)
    // ==========================================
    [Serializable]
    public class DurabilityComponent : ItemComponent
    {
        [Tooltip("The maximum durability this item can have.")]
        public int MaxDurability = 100;

        // Unity types are allowed here!
        public ParticleSystem BreakEffectPrefab; 

        public override ItemComponentState CreateInitialState()
        {
            return new DurabilityState(MaxDurability);
        }
    }

    // ==========================================
    // 2. THE RUNTIME TRACKER (Live Data for Saving)
    // ==========================================
    [Serializable]
    public class DurabilityState : ItemComponentState
    {
        public int CurrentDurability;

        // RULE 3.2: REQUIRED FOR JSON DESERIALIZATION!
        public DurabilityState() { }

        // Standard constructor used by CreateInitialState
        public DurabilityState(int startingDurability)
        {
            CurrentDurability = startingDurability;
        }

        public override bool IsStackableWith(ItemComponentState other)
        {
            if (other is DurabilityState otherDurability)
            {
                // RULE 3.3: Value-based comparison
                return this.CurrentDurability == otherDurability.CurrentDurability;
            }
            return false;
        }

        public override ItemComponentState Clone()
        {
            // RULE 3.4: Deep copy
            return new DurabilityState(this.CurrentDurability);
        }
    }
}