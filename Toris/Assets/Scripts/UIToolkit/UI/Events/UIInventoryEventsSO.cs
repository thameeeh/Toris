using UnityEngine;
using UnityEngine.Events;


namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UIInventoryEventsSO")]
    public class UIInventoryEventsSO : ScriptableObject
    {
        public UnityAction OnInventoryUpdated;
    }
}