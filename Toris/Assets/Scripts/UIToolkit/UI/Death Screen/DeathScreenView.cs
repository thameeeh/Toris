using System;
using System.Text;
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
        private Label _lostItemsLabel;
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
            _lostItemsLabel = Root.Q<Label>("Penalty_ItemList");
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
                SetLabelText(_experienceLostLabel, "XP Lost: calculating...");
                SetLabelText(_goldLostLabel, "Gold Lost: calculating...");
                SetLabelText(_itemsLostLabel, "Items Lost: calculating...");
                SetLabelText(_lostItemsLabel, "Penalty details will appear when available.");
                return;
            }

            SetLabelText(_experienceLostLabel, $"XP Lost: {summary.ExperienceLost:0.#}");
            SetLabelText(_goldLostLabel, $"Gold Lost: {summary.GoldLost}");
            SetLabelText(_itemsLostLabel, $"Items Lost: {summary.TotalItemsLost}");
            SetLabelText(_lostItemsLabel, BuildLostItemsText(summary));
        }

        private static string BuildLostItemsText(DeathPenaltySummary summary)
        {
            if (summary == null || summary.LostItems == null || summary.LostItems.Count == 0)
                return "No item loss.";

            const int maxVisibleRows = 5;
            StringBuilder builder = new StringBuilder();
            int visibleRows = Math.Min(maxVisibleRows, summary.LostItems.Count);

            for (int i = 0; i < visibleRows; i++)
            {
                DeathItemLossSummary item = summary.LostItems[i];
                if (i > 0)
                {
                    builder.AppendLine();
                }

                builder.Append(item.DisplayName);
                builder.Append(" x");
                builder.Append(item.Count);
                builder.Append(" (");
                builder.Append(item.Source == DeathPenaltyInventorySource.Potion ? "Potion" : "Backpack");
                builder.Append(')');
            }

            int hiddenRows = summary.LostItems.Count - visibleRows;
            if (hiddenRows > 0)
            {
                builder.AppendLine();
                builder.Append("+");
                builder.Append(hiddenRows);
                builder.Append(" more");
            }

            return builder.ToString();
        }

        private static void SetLabelText(Label label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
