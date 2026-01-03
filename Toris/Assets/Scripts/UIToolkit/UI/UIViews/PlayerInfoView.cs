using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit 
{ 
    public class PlayerInfoView : UIView
    {
        private Label _nameLabel;
        private Label _healthLabel;
        private Button _healButton;

        public PlayerInfoView(VisualElement topElement) : base(topElement) 
        {
            PlayerEvents.OnDataChanged += UpdateDisplay;
        }

        public override void Dispose()
        {
            PlayerEvents.OnDataChanged -= UpdateDisplay;
        }

        protected override void SetVisualElements()
        {
            _nameLabel = m_TopElement.Q<Label>("PlayerName");
            _healthLabel = m_TopElement.Q<Label>("PlayerHealth");
            _healButton = m_TopElement.Q<Button>("Heal-Button");
        }

        public void UpdateDisplay(PlayerDataSO data)
        {
            _nameLabel.text = data.NameDisplay;
            _healthLabel.text = data.HealthDisplay;
        }

        protected override void RegisterButtonCallbacks()
        {
            _healButton.RegisterCallback<ClickEvent>(SelectHealButton);
        }

        void SelectHealButton(ClickEvent evt)
        {
            PlayerEvents.OnHealRequested?.Invoke(10);
        }
    }
}