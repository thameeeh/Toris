using UnityEngine.UIElements;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{

    public class PlayerController : MonoBehaviour
    {
        public PlayerDataSO dataAsset;
        private PlayerInfoView _view;

        void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();

            _view = new PlayerInfoView(uiDoc.rootVisualElement);
            _view.Show();
            _view.UpdateDisplay(dataAsset);

            PlayerEvents.OnHealRequested += HandleHealRequested;
        }

        private void OnDisable()
        {
            PlayerEvents.OnHealRequested -= HandleHealRequested;
        }

        void HandleHealRequested(int amount)
        {
            dataAsset.AddHealth(amount);
        }
    }
}