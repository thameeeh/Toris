using OutlandHaven.UIToolkit;
using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{


    [Serializable]
    public class UpgradeableComponent : ItemComponent
    {
        public int MaxLevel = 5;

        // Future-proofing: If upgrading requires specific materials later,
        // you would define the material costs or references here, NOT in the state.

        public override ItemComponentState CreateInitialState()
        {
            // All new items start at level 1 by default
            return new UpgradeableState(1);
        }
    }

    [Serializable]
    public class UpgradeableState : ItemComponentState
    {
        public int CurrentLevel;

        public UpgradeableState(int startLevel)
        {
            CurrentLevel = startLevel;
        }

        public override bool IsStackableWith(ItemComponentState other)
        {
            if (other is UpgradeableState otherUpgrade)
            {
                // Items can only stack if they are the exact same level
                return this.CurrentLevel == otherUpgrade.CurrentLevel;
            }
            return false;
        }

        public override ItemComponentState Clone()
        {
            return new UpgradeableState { CurrentLevel = this.CurrentLevel };
        }
    }
}