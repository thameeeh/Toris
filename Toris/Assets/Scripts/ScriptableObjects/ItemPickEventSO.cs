using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{

    [CreateAssetMenu(fileName = "ItemPickEventSO", menuName = "Outland Haven/Inventory/Item Pick Event")]
    public class ItemPickEventSO : ScriptableObject
    {
        public Action OnItemPick;
    }

}