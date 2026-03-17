using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{

    [CreateAssetMenu(fileName = "ItemPickEventSO", menuName = "Scriptable Objects/ItemPickEventSO")]
    public class ItemPickEventSO : ScriptableObject
    {
        public Action OnItemPick;
    }

}