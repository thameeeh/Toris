using OutlandHaven.UIToolkit;
using OutlandHaven.Tutorial;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OutlandHaven.Inventory
{
    public class PlayerInventoryView : GameView, IDisposable
    {
        public override ScreenType ID => ScreenType.Inventory;
        private const string StatsDrawerOpenClass = "player-inventory-container--stats-open";
        private const string StatsToggleOpenClass = "inventory-stats-toggle--open";
        private const string TutorialItemAnchorPrefix = "inventory.item.";
        private const string StatsToggleTutorialAnchorId = "inventory.stats_toggle";
        private const string StatsPanelTutorialAnchorId = "inventory.stats_panel";
        private const string PotionAssignmentAreaTutorialAnchorId = "inventory.potion_assignment_area";

        private VisualTreeAsset _slotTemplate;
        private GameSessionSO _gameSession;

        private Dictionary<InventorySlot, InventorySlotView> _slotDictionary = new Dictionary<InventorySlot, InventorySlotView>();
        private Dictionary<InventorySlot, VisualElement> _slotVisualDictionary = new Dictionary<InventorySlot, VisualElement>();
        private Dictionary<string, VisualElement> _tutorialItemAnchors = new Dictionary<string, VisualElement>();

        // UI Containers
        private VisualElement _playerInventoryContainer;
        private VisualElement _playerGrid;
        private VisualElement _statsPanel;
        private Button _statsToggleButton;
        private PlayerEquipmentView _equipmentView;
        private PlayerStatsView _statsView;
        private PlayerPotionView _playerPotionView;
        private PlayerHUDBridge _hudBridge;
        private InventoryManager _equipmentInventory;
        private InventoryManager _potionInventory;

        private UIInventoryEventsSO _uiInventoryEvents;
        private bool _eventsBound = false;
        private InventoryInteractionContext _currentContext = InventoryInteractionContext.Normal;
        public event Action OnStatsDrawerToggleRequested;

        public PlayerInventoryView(
            VisualElement topElement, 
            VisualTreeAsset slotTemplate, 
            GameSessionSO session, 
            UIEventsSO uiEvents, 
            UIInventoryEventsSO uiInventoryEvents, 
            InventoryManager equipmentInventory = null, 
            InventoryManager potionInventory = null, 
            PlayerHUDBridge hudBridge = null)
            : base(topElement, uiEvents)
        {
            _slotTemplate = slotTemplate;
            _gameSession = session;
            _uiInventoryEvents = uiInventoryEvents;
            _equipmentInventory = equipmentInventory;
            _potionInventory = potionInventory; // Fixed: Now correctly assigned
            _hudBridge = hudBridge;
        }

        public override void Initialize()
        {
            base.Initialize();
            _equipmentView = new PlayerEquipmentView(m_TopElement, _slotTemplate, _uiInventoryEvents);
            _equipmentView.Initialize();
            _statsView = new PlayerStatsView(m_TopElement);
            _statsView.Initialize();

            _playerPotionView = new PlayerPotionView(m_TopElement, _slotTemplate, _uiInventoryEvents);
            _playerPotionView.Initialize();
        }

        public override void Show()
        {
            base.Show();
            if (!_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated += OnInventoryUpdated;
                _uiInventoryEvents.OnInteractionContextChanged += HandleContextChanged;

                _uiInventoryEvents.OnSpecificSlotsUpdated += HandleSpecificSlotsUpdated;
                _eventsBound = true;
            }
            _equipmentView?.Show();
            _statsView?.Show();
            _playerPotionView?.Show();
        }

        public override void Hide()
        {
            base.Hide();
            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _uiInventoryEvents.OnInteractionContextChanged -= HandleContextChanged;

                _uiInventoryEvents.OnSpecificSlotsUpdated -= HandleSpecificSlotsUpdated;
                _eventsBound = false;
            }
            _equipmentView?.Hide();
            _statsView?.Hide();
            _playerPotionView?.Hide();
        }

        protected override void SetVisualElements()
        {
            // Find the grids where slots live
            _playerInventoryContainer = m_TopElement.Q<VisualElement>("container__player");
            _playerGrid = m_TopElement.Q<VisualElement>("grid-player");
            _statsPanel = m_TopElement.Q<VisualElement>("Stats__Panel");
            _statsToggleButton = m_TopElement.Q<Button>("Btn_ToggleStats");
        }

        protected override void RegisterButtonCallbacks()
        {
            if (_statsToggleButton != null)
            {
                _statsToggleButton.clicked += HandleStatsToggleClicked;
            }

            RegisterTutorialAnchors();
        }

        public void SetStatsDrawerOpen(bool isOpen)
        {
            _playerInventoryContainer?.EnableInClassList(StatsDrawerOpenClass, isOpen);
            _statsToggleButton?.EnableInClassList(StatsToggleOpenClass, isOpen);
        }

        private void HandleStatsToggleClicked()
        {
            OnStatsDrawerToggleRequested?.Invoke();
        }

        void OnInventoryUpdated() 
        {
            RefreshGrid(_playerGrid, _gameSession.PlayerInventory);
        }

        public override void Setup(object payload)
        {
            // Refresh Player Inventory (Always)
            RefreshGrid(_playerGrid, _gameSession.PlayerInventory); 
            _equipmentView?.Setup(_equipmentInventory);
            _statsView?.Setup(_hudBridge);

            if (_potionInventory != null)
            {
                _playerPotionView?.Setup(_potionInventory);
            }
        }

        private void RefreshGrid(VisualElement gridRoot, InventoryManager data)
        {
            if (gridRoot == null) return;
            UnregisterTutorialItemAnchors();
            gridRoot.Clear();

            _slotDictionary.Clear();
            _slotVisualDictionary.Clear();

            if (data == null || data.LiveSlots == null) return;

            // Loop through data and create visuals
            foreach (var slotData in data.LiveSlots)
            {
                // Instantiate the UXML Template
                TemplateContainer slotInstance = _slotTemplate.Instantiate();
                slotInstance.pickingMode = PickingMode.Ignore; // Ensure the wrapper ignores picking
                gridRoot.Add(slotInstance);

                // Initialize the wrapper and update it
                // We pass in the owning InventoryManager (data) and the UI events
                var slotView = new InventorySlotView(slotInstance, data, _uiInventoryEvents);

                slotView.OnLocalClicked += (slot) => _uiInventoryEvents.OnItemClicked?.Invoke(slot);
                slotView.OnLocalRightClicked += HandleMainInventoryRightClick;
                slotView.OnLocalMoveItemRequested += (sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove) => _uiInventoryEvents.OnRequestMoveItem?.Invoke(sourceContainer, sourceSlot, targetContainer, targetSlot, amountToMove);
                slotView.OnLocalSelectForProcessingRequested += (slot, proxyID) => _uiInventoryEvents.OnRequestSelectForProcessing?.Invoke(slot, proxyID);

                slotView.OnLocalDragStarted += (sprite, pos, size) => _uiInventoryEvents.OnGlobalDragStarted?.Invoke(sprite, pos, size);
                slotView.OnLocalDragUpdated += (pos) => _uiInventoryEvents.OnGlobalDragUpdated?.Invoke(pos);
                slotView.OnLocalDragStopped += () => _uiInventoryEvents.OnGlobalDragStopped?.Invoke();
                slotView.Update(slotData);
                // Click events are now handled natively inside InventorySlotView via PointerUpEvent
                
                //view is being saved into the dictionary using the data slot as the key
                _slotDictionary.Add(slotData, slotView);
                _slotVisualDictionary.Add(
                    slotData,
                    slotInstance.Q<VisualElement>(className: "item-slot") ?? slotInstance);
            }

            RefreshTutorialItemAnchors();
        }

        private void HandleSpecificSlotsUpdated(InventorySlot sourceSlot, InventorySlot targetSlot)
        {
            // If the source slot belongs to this grid, update its visual
            if (sourceSlot != null && _slotDictionary.TryGetValue(sourceSlot, out var sourceView))
            {
                sourceView.Update(sourceSlot);
            }

            // If the target slot belongs to this grid, update its visual
            if (targetSlot != null && _slotDictionary.TryGetValue(targetSlot, out var targetView))
            {
                targetView.Update(targetSlot);
            }

            RefreshTutorialItemAnchors();
        }

        private void RefreshTutorialItemAnchors()
        {
            UnregisterTutorialItemAnchors();

            foreach (KeyValuePair<InventorySlot, VisualElement> slotVisual in _slotVisualDictionary)
            {
                InventorySlot slot = slotVisual.Key;
                InventoryItemSO item = slot?.HeldItem?.BaseItem;
                if (slot == null || slot.IsEmpty || item == null || string.IsNullOrWhiteSpace(item.ItemName))
                    continue;

                string anchorId = TutorialItemAnchorPrefix + item.ItemName.Trim().ToLowerInvariant();
                if (_tutorialItemAnchors.ContainsKey(anchorId))
                    continue;

                TutorialAnchorRegistry.Register(anchorId, slotVisual.Value);
                _tutorialItemAnchors.Add(anchorId, slotVisual.Value);
            }
        }

        private void UnregisterTutorialItemAnchors()
        {
            foreach (KeyValuePair<string, VisualElement> tutorialAnchor in _tutorialItemAnchors)
                TutorialAnchorRegistry.Unregister(tutorialAnchor.Key, tutorialAnchor.Value);

            _tutorialItemAnchors.Clear();
        }

        private void RegisterTutorialAnchors()
        {
            // Cross-system boundary: Inventory exposes stable bounds only.
            // Tutorial sequencing and completion stay in Scripts/Tutorial.
            TutorialAnchorRegistry.Register(StatsToggleTutorialAnchorId, _statsToggleButton);
            TutorialAnchorRegistry.Register(StatsPanelTutorialAnchorId, _statsPanel);
            TutorialAnchorRegistry.Register(PotionAssignmentAreaTutorialAnchorId, _playerInventoryContainer);
        }

        private void UnregisterTutorialAnchors()
        {
            TutorialAnchorRegistry.Unregister(StatsToggleTutorialAnchorId, _statsToggleButton);
            TutorialAnchorRegistry.Unregister(StatsPanelTutorialAnchorId, _statsPanel);
            TutorialAnchorRegistry.Unregister(PotionAssignmentAreaTutorialAnchorId, _playerInventoryContainer);
        }

        private void HandleContextChanged(InventoryInteractionContext newContext)
        {
            _currentContext = newContext;
        }

        private void HandleMainInventoryRightClick(InventorySlot dataSlot)
        {
            switch (_currentContext)
            {
                case InventoryInteractionContext.Shop:
                    _uiInventoryEvents.OnRequestSell?.Invoke(dataSlot.HeldItem, 1);
                    break;
                case InventoryInteractionContext.Forge:
                    _uiInventoryEvents.OnItemClicked?.Invoke(dataSlot);
                    break;
                case InventoryInteractionContext.Salvage:
                    _uiInventoryEvents.OnItemClicked?.Invoke(dataSlot);
                    break;
                case InventoryInteractionContext.Normal:
                default:
                    if (dataSlot == null || dataSlot.IsEmpty || dataSlot.HeldItem?.BaseItem == null)
                        return;

                    if (dataSlot.HeldItem.BaseItem.GetComponent<ConsumableComponent>() != null)
                    {
                        _uiInventoryEvents.OnRequestUse?.Invoke(dataSlot);
                    }
                    else if (dataSlot.HeldItem.BaseItem.GetComponent<EquipableComponent>() != null)
                    {
                        _uiInventoryEvents.OnRequestEquip?.Invoke(dataSlot);
                    }
                    break;
            }
        }

        public override void Dispose()
        {
            if (_statsToggleButton != null)
            {
                _statsToggleButton.clicked -= HandleStatsToggleClicked;
            }

            if (_eventsBound && _uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnInventoryUpdated -= OnInventoryUpdated;
                _uiInventoryEvents.OnInteractionContextChanged -= HandleContextChanged;
                _uiInventoryEvents.OnSpecificSlotsUpdated -= HandleSpecificSlotsUpdated;
                _eventsBound = false;
            }
            _equipmentView?.Dispose();
            _statsView?.Dispose();

            _playerPotionView?.Dispose();
            UnregisterTutorialAnchors();
            UnregisterTutorialItemAnchors();

            base.Dispose();
        }
    }
}
