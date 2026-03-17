using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.Events;

namespace OutlandHaven.UIToolkit
{

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UIEventsSO")]
    public class UIEventsSO : ScriptableObject
    {
        public UnityAction<ScreenType, object> OnRequestOpen; //for inventory, pass container with items' data as object

        public UnityAction<ScreenType> OnRequestClose;

        public UnityAction OnRequestCloseAll;

        public UnityAction<ScreenType> OnScreenOpen;
    }

}