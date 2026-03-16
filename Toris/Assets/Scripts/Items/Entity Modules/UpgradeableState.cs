using System;

namespace OutlandHaven.UIToolkit
{
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
    }
}
