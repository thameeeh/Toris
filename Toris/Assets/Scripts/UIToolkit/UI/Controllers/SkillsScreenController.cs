using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public class SkillsScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _skillsMainTemplate; // <--- Drag SkillScreen.uxml here
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private PlayerHUDBridge _playerHudBridge;

        private SkillsView _view;
        private UIManager _uiManager;

        private void Awake()
        {
            _uiManager = FindFirstObjectByType<UIManager>();
            if (_playerHudBridge == null)
            {
                _playerHudBridge = FindFirstObjectByType<PlayerHUDBridge>();
            }
        }

        private void Start()
        {
            if (_skillsMainTemplate == null)
            {
                Debug.LogError("SkillsScreenController: Main Template is missing!");
                return;
            }

            // 1. Instantiate the UI from the asset
            TemplateContainer skillsInstance = _skillsMainTemplate.Instantiate();

            // Allow it to grow to fill the parent container completely
            skillsInstance.style.flexGrow = 1;

            // 2. Pass the instance to the View
            _view = new SkillsView(skillsInstance, _uiEvents);
            _view.Initialize();

            // 3. Register to the FullScreen zone
            _uiManager.RegisterView(_view, ScreenZone.FullScreen);
        }

        private void OnEnable()
        {
            // Note: Explicitly avoiding UIManager interactions here to prevent race conditions as requested.
            // The instantiation and registration happens in Start().

            if (_uiEvents != null)
            {
                _uiEvents.OnScreenOpen += HandleScreenOpen;
            }
        }

        private void OnDisable()
        {
            if (_uiEvents != null)
            {
                _uiEvents.OnScreenOpen -= HandleScreenOpen;
            }
        }

        private void HandleScreenOpen(ScreenType type)
        {
            if (type == ScreenType.SkillScreen)
            {
                // Push data to the view when the screen is opened
                UpdateViewData();
            }
        }

        private void UpdateViewData()
        {
            if (_view == null) return;

            // Currently, the PlayerHUDBridge does not have these stats,
            // so we send placeholder data for now as per the requirements.
            SkillsPayload dummyPayload = new SkillsPayload
            {
                Strength = 10,
                StrengthXpPercentage = 50f,
                Agility = 15,
                AgilityXpPercentage = 75f,
                Intelligence = 20,
                IntelligenceXpPercentage = 25f
            };

            _view.Setup(dummyPayload);
        }

        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing UI Events SO", this);
            }
        }
    }
}
