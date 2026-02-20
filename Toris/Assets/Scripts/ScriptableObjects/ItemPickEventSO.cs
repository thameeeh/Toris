using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemPickEventSO", menuName = "Scriptable Objects/ItemPickEventSO")]
public class ItemPickEventSO : ScriptableObject
{
    public Action OnItemPick;
}
