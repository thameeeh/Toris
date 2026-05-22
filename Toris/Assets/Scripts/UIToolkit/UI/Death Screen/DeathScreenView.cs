using System;
using System.Collections.Generic;
using OutlandHaven.Inventory;
using UnityEngine;
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
        private Label _causeOfDeathLabel;
        private VisualElement _lostItemsList;
        private UIInventoryEventsSO _uiInventoryEvents;
        private readonly List<InventorySlot> _lostItemTooltipSlots = new List<InventorySlot>();
        private bool _eventsBound;

        public event Action OnRespawnClicked;
        public event Action OnMainMenuClicked;

        public DeathScreenView(
            VisualElement topElement,
            UIEventsSO uiEvents,
            UIInventoryEventsSO uiInventoryEvents) : base(topElement, uiEvents)
        {
            _uiInventoryEvents = uiInventoryEvents;
        }

        protected override void SetVisualElements()
        {
            _respawnButton = Root.Q<Button>("Btn_Respawn");
            _mainMenuButton = Root.Q<Button>("Btn_MainMenu");
            _experienceLostLabel = Root.Q<Label>("Penalty_XP");
            _goldLostLabel = Root.Q<Label>("Penalty_Gold");
            _itemsLostLabel = Root.Q<Label>("Penalty_Items");
            _causeOfDeathLabel = Root.Q<Label>("Penalty_CauseOfDeath");
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
            HideTooltip();
            UnbindEvents();
            base.Hide();
        }

        public override void Dispose()
        {
            HideTooltip();
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
                SetLabelText(_causeOfDeathLabel, DeathCauseSnapshot.UnknownDisplayName);
                RebuildLostItemRows(null);
                return;
            }

            SetLabelText(_experienceLostLabel, $"{summary.ExperienceLost:0.#} XP");
            SetLabelText(_goldLostLabel, summary.GoldLost.ToString());
            SetLabelText(_itemsLostLabel, summary.TotalItemsLost.ToString());
            SetLabelText(_causeOfDeathLabel, summary.CauseOfDeath);
            RebuildLostItemRows(summary);
        }

        private void RebuildLostItemRows(DeathPenaltySummary summary)
        {
            if (_lostItemsList == null)
                return;

            // Death screen related: item loss rows are display-only and mirror the
            // already-calculated penalty summary from gameplay code.
            _lostItemsList.Clear();
            _lostItemTooltipSlots.Clear();

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

            for (int i = 0; i < summary.LostItems.Count; i++)
            {
                DeathItemLossSummary item = summary.LostItems[i];
                if (item.ItemBlueprint != null)
                {
                    AddLostItemCard(item);
                    continue;
                }

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
        }

        private void AddLostItemCard(DeathItemLossSummary item)
        {
            InventorySlot proxySlot = CreateTooltipSlot(item);
            _lostItemTooltipSlots.Add(proxySlot);

            VisualElement card = new VisualElement();
            card.AddToClassList("death-screen__item-card");
            card.AddToClassList(item.Source == DeathPenaltyInventorySource.Potion
                ? "death-screen__item-card--potion"
                : "death-screen__item-card--backpack");

            Image icon = new Image();
            icon.AddToClassList("death-screen__item-card-icon");
            icon.sprite = item.ItemBlueprint.Icon;
            icon.scaleMode = ScaleMode.ScaleToFit;
            icon.pickingMode = PickingMode.Ignore;

            Label count = new Label($"x{item.Count}");
            count.AddToClassList("death-screen__item-card-count");
            count.pickingMode = PickingMode.Ignore;

            card.Add(icon);
            card.Add(count);
            card.RegisterCallback<PointerEnterEvent>(evt => ShowTooltip(proxySlot, evt.position));
            card.RegisterCallback<PointerMoveEvent>(evt => MoveTooltip(evt.position));
            card.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
            _lostItemsList.Add(card);
        }

        private static InventorySlot CreateTooltipSlot(DeathItemLossSummary item)
        {
            InventorySlot slot = new InventorySlot();
            ItemInstance tooltipItem = new ItemInstance(item.ItemBlueprint)
            {
                InstanceID = item.ItemId
            };

            slot.SetItem(tooltipItem, item.Count);

            return slot;
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

        private void ShowTooltip(InventorySlot slot, Vector2 pointerPosition)
        {
            if (slot == null || slot.IsEmpty)
                return;

            _uiInventoryEvents?.OnItemTooltipShow?.Invoke(slot, pointerPosition);
        }

        private void MoveTooltip(Vector2 pointerPosition)
        {
            _uiInventoryEvents?.OnItemTooltipMove?.Invoke(pointerPosition);
        }

        private void HideTooltip()
        {
            _uiInventoryEvents?.OnItemTooltipHide?.Invoke();
        }
    }
}
