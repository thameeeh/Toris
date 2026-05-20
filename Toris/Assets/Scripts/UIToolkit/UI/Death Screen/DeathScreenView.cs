using System;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    // Dumb MVP view for the death screen. It only exposes button intents.
    public sealed class DeathScreenView : GameView
    {
        public override ScreenType ID => ScreenType.DeathScreen;

        private Button _respawnButton;
        private Button _mainMenuButton;
        private Label _experienceLostLabel;
        private Label _goldLostLabel;
        private Label _itemsLostLabel;
        private VisualElement _lostItemsList;
        private bool _eventsBound;

        public event Action OnRespawnClicked;
        public event Action OnMainMenuClicked;

        public DeathScreenView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents)
        {
        }

        protected override void SetVisualElements()
        {
            _respawnButton = Root.Q<Button>("Btn_Respawn");
            _mainMenuButton = Root.Q<Button>("Btn_MainMenu");
            _experienceLostLabel = Root.Q<Label>("Penalty_XP");
            _goldLostLabel = Root.Q<Label>("Penalty_Gold");
            _itemsLostLabel = Root.Q<Label>("Penalty_Items");
            _lostItemsList = Root.Q<VisualElement>("Penalty_ItemList");
        }

        public override void Setup(object payload = null)
        {
            ApplyPenaltySummary(payload as DeathPenaltySummary);
        }

        public override void Show()
        {
            base.Show();
            BindEvents();
            _respawnButton?.Focus();
        }

        public override void Hide()
        {
            UnbindEvents();
            base.Hide();
        }

        public override void Dispose()
        {
            UnbindEvents();
            base.Dispose();
        }

        private void BindEvents()
        {
            if (_eventsBound)
                return;

            if (_respawnButton != null)
                _respawnButton.clicked += HandleRespawnClicked;

            if (_mainMenuButton != null)
                _mainMenuButton.clicked += HandleMainMenuClicked;

            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound)
                return;

            if (_respawnButton != null)
                _respawnButton.clicked -= HandleRespawnClicked;

            if (_mainMenuButton != null)
                _mainMenuButton.clicked -= HandleMainMenuClicked;

            _eventsBound = false;
        }

        private void HandleRespawnClicked() => OnRespawnClicked?.Invoke();

        private void HandleMainMenuClicked() => OnMainMenuClicked?.Invoke();

        private void ApplyPenaltySummary(DeathPenaltySummary summary)
        {
            // Death screen related: the view only formats the penalty report it receives.
            if (summary == null)
            {
                SetLabelText(_experienceLostLabel, "calculating...");
                SetLabelText(_goldLostLabel, "calculating...");
                SetLabelText(_itemsLostLabel, "calculating...");
                RebuildLostItemRows(null);
                return;
            }

            SetLabelText(_experienceLostLabel, $"{summary.ExperienceLost:0.#} XP");
            SetLabelText(_goldLostLabel, summary.GoldLost.ToString());
            SetLabelText(_itemsLostLabel, summary.TotalItemsLost.ToString());
            RebuildLostItemRows(summary);
        }

        private void RebuildLostItemRows(DeathPenaltySummary summary)
        {
            if (_lostItemsList == null)
                return;

            // Death screen related: item loss rows are display-only and mirror the
            // already-calculated penalty summary from gameplay code.
            _lostItemsList.Clear();

            if (summary == null)
            {
                AddLostItemMessage("Penalty details will appear when available.");
                return;
            }

            if (summary.LostItems == null || summary.LostItems.Count == 0)
            {
                AddLostItemMessage("No item loss.");
                return;
            }

            const int maxVisibleRows = 6;
            int visibleRows = Math.Min(maxVisibleRows, summary.LostItems.Count);

            for (int i = 0; i < visibleRows; i++)
            {
                DeathItemLossSummary item = summary.LostItems[i];
                VisualElement row = new VisualElement();
                row.AddToClassList("death-screen__item-row");

                Label name = new Label($"{item.DisplayName} x{item.Count}");
                name.AddToClassList("death-screen__item-name");

                Label source = new Label(item.Source == DeathPenaltyInventorySource.Potion ? "Potion" : "Backpack");
                source.AddToClassList("death-screen__item-source");

                row.Add(name);
                row.Add(source);
                _lostItemsList.Add(row);
            }

            int hiddenRows = summary.LostItems.Count - visibleRows;
            if (hiddenRows > 0)
            {
                AddLostItemMessage($"+{hiddenRows} more");
            }
        }

        private static void SetLabelText(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private void AddLostItemMessage(string text)
        {
            Label label = new Label(text);
            label.AddToClassList("death-screen__item-message");
            _lostItemsList.Add(label);
        }
    }
}
