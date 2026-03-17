using System;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    // --- THE BLUEPRINT (Static Rules) ---
    [Serializable]
    public class EvolvingComponent : ItemComponent
    {
        [Tooltip("How many kills are required to awaken the weapon?")]
        public int KillsRequired = 50;

        [Tooltip("The flat damage bonus applied when awakened.")]
        public float AwakenedDamageBonus = 15f;

        // We need a runtime tracker, so we generate the state here.
        public override ItemComponentState CreateInitialState()
        {
            return new EvolvingState();
        }
    }

    // --- THE RUNTIME TRACKER (Live Data) ---
    [Serializable]
    public class EvolvingState : ItemComponentState
    {
        public int CurrentKills;
        public bool IsAwakened;

        public EvolvingState()
        {
            CurrentKills = 0;
            IsAwakened = false;
        }

        // A helper method to handle the logic of adding a kill
        public void AddKill(int requiredKills)
        {
            if (IsAwakened) return; // Already maxed out

            CurrentKills++;

            if (CurrentKills >= requiredKills)
            {
                IsAwakened = true;
                CurrentKills = requiredKills; // Cap it
            }
        }

        public override bool IsStackableWith(ItemComponentState other)
        {
            if (other is EvolvingState otherEvolvingState)
            {
                // Weapons with kill trackers usually shouldn't stack.
                // But if they do, they must have the exact same kill count.
                return this.CurrentKills == otherEvolvingState.CurrentKills
                    && this.IsAwakened == otherEvolvingState.IsAwakened;
            }
            return false;
        }
    }
}