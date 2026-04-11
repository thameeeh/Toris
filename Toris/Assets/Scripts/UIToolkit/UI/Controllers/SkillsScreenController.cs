using UnityEngine;
using UnityEngine.UIElements;
using OutlandHaven.Skills; // Make sure to include the namespace where PlayerSkillView and SkillData live

namespace OutlandHaven.UIToolkit
{
    public class SkillsScreenController : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private VisualTreeAsset _skillsMainTemplate; // <--- Drag SkillScreen.uxml here
        [SerializeField] private UIEventsSO _uiEvents;
        [SerializeField] private PlayerHUDBridge _playerHudBridge;

        [Header("Database")]
        [Tooltip("Drop all your SkillData ScriptableObjects here")]
        [SerializeField] private SkillData[] _skillDatabase;

        private PlayerSkillView _view; // Updated to the new view
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

            // 2. Pass the instance, UI events, AND the new database to the View
            _view = new PlayerSkillView(skillsInstance, _uiEvents, _skillDatabase);
            _view.Initialize();

            // 3. Register to the FullScreen zone
            _uiManager.RegisterView(_view, ScreenZone.FullScreen);
        }

        private void OnEnable()
        {
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
            // Make sure this matches the enum you added (e.g., ScreenType.Skills)
            if (type == ScreenType.Skills)
            {
                // Push data to the view when the screen is opened
                UpdateViewData();
            }
        }

        private void UpdateViewData()
        {
            if (_view == null) return;

            // Kept your dummy payload for future integration
            SkillsPayload dummyPayload = new SkillsPayload
            {
                Strength = 10,
                StrengthXpPercentage = 50f,
                Agility = 15,
                AgilityXpPercentage = 75f,
                Intelligence = 20,
                IntelligenceXpPercentage = 25f
            };

            // This will pass the payload into the Setup() method of PlayerSkillView
            _view.Setup(dummyPayload);
        }

        private void OnValidate()
        {
            if (_uiEvents == null)
            {
                Debug.LogError($" <color=red>{name}</color> missing UI Events SO", this);
            }
            if (_skillDatabase == null || _skillDatabase.Length == 0)
            {
                Debug.LogWarning($"<color=yellow>{name}</color> has an empty Skill Database. Don't forget to assign your ScriptableObjects!", this);
            }
        }
    }
}