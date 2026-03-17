using UnityEngine;

namespace OutlandHaven.Inventory
{

    [System.Serializable]
    public class ConsumableComponent : ItemComponent
    {
        public string ItemsName = "Consumable Component";
        public string EffectPayload;
        public string Cooldown;
    }

}