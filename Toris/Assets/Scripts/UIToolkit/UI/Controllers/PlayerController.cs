using UnityEngine.UIElements;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{

    public class PlayerController : MonoBehaviour
    {
        public PlayerDataSO dataAsset;

        public VisualTreeAsset statRowTemplate;

        private PlayerInfoView _view;

        void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();

            _view = new PlayerInfoView(uiDoc.rootVisualElement);
            _view.Root.dataSource = dataAsset;

            GenerateStatList();

            _view.Show();

            PlayerEvents.OnHealRequested += HandleHealRequested;
        }

        void GenerateStatList() 
        {
            if (_view.StatContainer == null || statRowTemplate == null || dataAsset == null)
                return;

            _view.StatContainer.Clear();

            foreach (var stat in dataAsset.Stats)
            {
                var statRow = statRowTemplate.Instantiate();
             
                statRow.dataSource = stat;
                
                _view.StatContainer.Add(statRow);
            }
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