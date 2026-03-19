using UnityEngine;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Inventory
{

    public class EvolvingWeaponDebugger : MonoBehaviour
    {
        public InventoryItemSO CursedDaggerBlueprint;

        void Start()
        {
            // 1. Generate the live item
            ItemInstance myDagger = new ItemInstance(CursedDaggerBlueprint);

            // 2. Grab the static rules and the live state
            var evolvingRules = myDagger.BaseItem.GetComponent<EvolvingComponent>();
            var evolvingState = myDagger.GetState<EvolvingState>();

            if (evolvingRules != null && evolvingState != null)
            {
                Debug.Log($"Looted dagger! Needs {evolvingRules.KillsRequired} kills to awaken.");

                // 3. Simulate killing 50 enemies
                for (int i = 0; i < 50; i++)
                {
                    evolvingState.AddKill(evolvingRules.KillsRequired);
                }

                // 4. Check the result
                if (evolvingState.IsAwakened)
                {
                    Debug.Log($"The dagger has AWAKENED! It now deals an extra {evolvingRules.AwakenedDamageBonus} damage.");
                }
            }
        }
    }
}