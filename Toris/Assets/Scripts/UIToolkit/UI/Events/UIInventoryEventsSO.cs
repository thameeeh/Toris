using UnityEngine;
using UnityEngine.Events;


namespace OutlandHaven.UIToolkit
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UIInventoryEventsSO")]
    public class UIInventoryEventsSO : ScriptableObject
    {
        public UnityAction OnInventoryUpdated;

        [Header("Shop Events")]
        public UnityAction OnShopInventoryUpdated;
        public UnityAction<InventoryItemSO, int> OnRequestBuy;
        public UnityAction<InventoryItemSO, int> OnRequestSell;
        public UnityAction<int> OnCurrencyChanged;
    }
}