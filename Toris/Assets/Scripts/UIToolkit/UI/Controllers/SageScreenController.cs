using OutlandHaven.Inventory;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SageUpgradeScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _sageUpgradeTemplate; // Drag SageUpgrade.uxml here
        [SerializeField] private VisualTreeAsset _sageUpgradeSubViewTemplate; // Drag UpgradeSubView_Sage.uxml here
        [SerializeField] private VisualTreeAsset _sageBrewTemplate; // Drag BrewSubView_Sage.uxml here
        [SerializeField] private VisualTreeAsset _slotTemplate; // Drag Slot.uxml here
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UpgradeSalvageManagerSO _upgradeManager;
        [SerializeField] private CraftingManagerSO _brewingManager; // Drag potion brewing CraftingManagerSO here

        private SageUpgradeView _view;
        private UIManager _uiManager;

        private void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            if (_gameSession == null)
            {
                _gameSession = GameSessionSO.LoadDefault();
            }
            if (_brewingManager != null)
            {
                _brewingManager.Initialize();
            }
        }

        private void OnEnable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen += HandleRequestOpen;
                _uiEvents.OnScreenOpen += HandleScreenOpen;
            }

            if (_uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnRequestSageUpgrade += HandleUpgradeRequest;
            }

            if (_sageUpgradeTemplate == null)
            {
                Debug.LogError("SageUpgradeScreenController: Sage Upgrade UXML Template is missing!");
            }
            if (_sageUpgradeSubViewTemplate == null)
            {
                Debug.LogError("SageUpgradeScreenController: Sage Upgrade SubView UXML Template is missing!");
            }
            if (_sageBrewTemplate == null)
            {
                Debug.LogError("SageUpgradeScreenController: Sage Brew UXML Template is missing!");
            }
            if (_slotTemplate == null)
            {
                Debug.LogError("SageUpgradeScreenController: Slot Template is missing!");
            }
            if (_brewingManager == null)
            {
                Debug.LogError("SageUpgradeScreenController: Brewing Manager SO is missing!");
            }
        }

        private void OnDisable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnRequestOpen -= HandleRequestOpen;
                _uiEvents.OnScreenOpen -= HandleScreenOpen;
            }

            if (_uiInventoryEvents != null)
            {
                _uiInventoryEvents.OnRequestSageUpgrade -= HandleUpgradeRequest;
            }
        }

        private void Start()
        {
            if (_sageUpgradeTemplate == null || _sageUpgradeSubViewTemplate == null || _sageBrewTemplate == null || _slotTemplate == null) return;
            if (_gameSession == null)
            {
                _gameSession = GameSessionSO.LoadDefault();
            }

            TemplateContainer sageInstance = _sageUpgradeTemplate.Instantiate();
            sageInstance.style.flexGrow = 1; // Fit inside the zone container

            _view = new SageUpgradeView(
                sageInstance,
                _sageUpgradeSubViewTemplate,
                _sageBrewTemplate,
                _slotTemplate,
                _uiEvents,
                _uiInventoryEvents,
                _gameSession,
                _upgradeManager,
                _brewingManager
            );
            _view.Initialize();

            if (_uiManager != null)
            {
                _uiManager.RegisterView(_view, ScreenZone.Left);
            }
            else
            {
                Debug.LogError("SageUpgradeScreenController: UIManager not found in scene! Cannot register view.");
            }
        }

        private void HandleRequestOpen(ScreenType screenType, object payload)
        {
            if (screenType != ScreenType.SageUpgrade) return;

            if (_view != null)
            {
                _view.Setup(null);
            }
        }

        private void HandleScreenOpen(ScreenType screenType)
        {
            if (screenType != ScreenType.SageUpgrade) return;

            EnsureInventoryVisible();
        }

        private void EnsureInventoryVisible()
        {
            if (_uiManager == null || _uiEvents == null) return;

            if (!_uiManager.IsWindowOpen(ScreenType.Inventory))
            {
                _uiEvents.OnRequestOpen?.Invoke(ScreenType.Inventory, null);
            }
        }

        private void HandleUpgradeRequest(InventorySlot slot)
        {
            if (_upgradeManager == null || slot == null || slot.IsEmpty) return;

            bool success = _upgradeManager.TryUpgradeItem(slot);
            if (success)
            {
                // Play confirm sound
                if (_uiEvents != null)
                {
                    _uiEvents.RequestSfx(_uiEvents.ButtonConfirmSfxId);
                }

                // Refresh UI stats
                _view?.Setup(null);
                
                // Notify the rest of the UI that inventory items changed
                _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
            }
            else
            {
                // Play fail sound or sfx if applicable
                Debug.LogWarning("SageUpgradeScreenController: Weapon upgrade failed.");
            }
        }

        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($"SageUpgradeScreenController {name} is missing UI Events SO in inspector!", this);
            }
        }
    }
}
