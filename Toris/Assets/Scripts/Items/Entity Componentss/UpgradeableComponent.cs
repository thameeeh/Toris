using System;

namespace OutlandHaven.UIToolkit
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
}
