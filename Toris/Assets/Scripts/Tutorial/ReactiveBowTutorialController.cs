using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class ReactiveBowTutorialController : MonoBehaviour
    {
        private const string DefaultUiEventsResourcePath = "GameData/SOForEvents/UI Events SO";
        private const string DefaultBowEventsResourcePath = "GameData/SOForEvents/Player Bow Events SO";
        private const string CapabilityLockId = "Tutorial.ReactiveBow";
        private const string UnderdrawStepId = "prologue.bow.dry_release";
        private const string OverdrawStepId = "prologue.bow.overdraw";
        private const float PromptFallbackWidth = 360f;
        private const float PromptFallbackHeight = 92f;

        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIEventsSO uiEvents;
        [SerializeField] private GameSessionSO gameSession;
        [SerializeField] private PlayerBowEventsSO bowEvents;

        [Header("Tip Content")]
        [SerializeField] private string underdrawTipText = "Release too early and the shot fails.";
        [SerializeField] private string overdrawTipText = "Holding too long makes the shot unstable.";
        [SerializeField] private string continueText = "Continue";

        [Header("Behavior")]
        [SerializeField] private bool pauseGameplay = true;
        [SerializeField] private GameplayInputCapability[] lockedCapabilitiesWhileVisible =
        {
            GameplayInputCapability.Movement,
            GameplayInputCapability.Combat,
            GameplayInputCapability.Interaction,
            GameplayInputCapability.Inventory,
            GameplayInputCapability.Skills,
            GameplayInputCapability.QuestJournal,
            GameplayInputCapability.PotionHotkeys,
            GameplayInputCapability.QuickSaveLoad
        };

        private VisualElement _promptRoot;
        private Label _promptLabel;
        private Label _continueLabel;
        private bool _eventsBound;
        private bool _isVisible;
        private bool _capabilitiesLocked;
        private bool _gameplayPaused;
        private float _previousTimeScale = 1f;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();
            BindEvents();
        }

        private void OnDisable()
        {
            UnbindEvents();
            HideTip();
        }

        private void Update()
        {
            if (!_isVisible)
                return;

            PositionPrompt();

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null
                && (keyboard.spaceKey.wasPressedThisFrame
                    || keyboard.enterKey.wasPressedThisFrame
                    || keyboard.numpadEnterKey.wasPressedThisFrame))
            {
                HideTip();
            }
        }

        private void HandleUnderdrawReleased(PlayerBowController source)
        {
            TryShowTip(UnderdrawStepId, underdrawTipText, "Release too early and the shot fails.", source, false);
        }

        private void HandleOverdrawStarted(PlayerBowController source)
        {
            TryShowTip(OverdrawStepId, overdrawTipText, "Holding too long makes the shot unstable.", source, true);
        }

        private void TryShowTip(
            string stepId,
            string configuredText,
            string fallbackText,
            PlayerBowController source,
            bool cancelCurrentDraw)
        {
            if (_isVisible
                || !AreTutorialTipsEnabled()
                || IsStepCompleted(stepId)
                || !EnsurePromptView())
            {
                return;
            }

            if (cancelCurrentDraw)
                source?.CancelCurrentDraw("ReactiveBowTutorial");

            _promptLabel.text = string.IsNullOrWhiteSpace(configuredText)
                ? fallbackText
                : configuredText.Trim();
            _continueLabel.text = string.IsNullOrWhiteSpace(continueText)
                ? "Continue"
                : continueText.Trim();

            _promptRoot.style.display = DisplayStyle.Flex;
            _promptRoot.style.opacity = 1f;
            _promptRoot.BringToFront();
            _isVisible = true;
            LockCapabilities();
            PauseGameplay();
            gameSession?.MarkTutorialStepCompleted(stepId);
            PositionPrompt();
        }

        private bool EnsurePromptView()
        {
            if (_promptRoot != null)
                return true;

            VisualElement host = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (host == null)
                return false;

            _promptRoot = new VisualElement { name = "ReactiveBowTutorialPrompt" };
            _promptRoot.AddToClassList("prologue-tutorial-prompt");
            _promptRoot.pickingMode = PickingMode.Ignore;

            _promptLabel = new Label { name = "ReactiveBowTutorialLabel" };
            _promptLabel.AddToClassList("prologue-tutorial-prompt__label");
            _promptRoot.Add(_promptLabel);

            VisualElement continueRoot = new VisualElement { name = "ReactiveBowTutorialContinue" };
            continueRoot.AddToClassList("prologue-tutorial-prompt__continue");
            _continueLabel = new Label { name = "ReactiveBowTutorialContinueLabel" };
            _continueLabel.AddToClassList("prologue-tutorial-prompt__continue-text");
            continueRoot.Add(_continueLabel);

            Label continueKeycap = new Label { name = "ReactiveBowTutorialKeycap", text = "Space" };
            continueKeycap.AddToClassList("prologue-tutorial-prompt__keycap");
            continueRoot.Add(continueKeycap);
            _promptRoot.Add(continueRoot);

            host.Add(_promptRoot);
            _promptRoot.style.display = DisplayStyle.None;
            return true;
        }

        private void PositionPrompt()
        {
            if (_promptRoot == null || _promptRoot.parent == null)
                return;

            VisualElement host = _promptRoot.parent;
            float hostWidth = ResolveDimension(host.resolvedStyle.width, Screen.width);
            float hostHeight = ResolveDimension(host.resolvedStyle.height, Screen.height);
            float promptWidth = ResolveDimension(_promptRoot.resolvedStyle.width, PromptFallbackWidth);
            float promptHeight = ResolveDimension(_promptRoot.resolvedStyle.height, PromptFallbackHeight);

            _promptRoot.style.left = Mathf.Max(0f, (hostWidth - promptWidth) * 0.5f);
            _promptRoot.style.top = Mathf.Max(0f, (hostHeight - promptHeight) * 0.35f);
        }

        private void HideTip()
        {
            _isVisible = false;
            if (_promptRoot != null)
                _promptRoot.style.display = DisplayStyle.None;

            ReleaseCapabilities();
            ReleasePause();
        }

        private void LockCapabilities()
        {
            if (_capabilitiesLocked || uiEvents == null || lockedCapabilitiesWhileVisible == null)
                return;

            // The lesson gates input only while its explanation is visible; gameplay rules remain elsewhere.
            for (int i = 0; i < lockedCapabilitiesWhileVisible.Length; i++)
                uiEvents.OnGameplayCapabilityLockRequested?.Invoke(lockedCapabilitiesWhileVisible[i], CapabilityLockId);

            _capabilitiesLocked = true;
        }

        private void ReleaseCapabilities()
        {
            if (!_capabilitiesLocked || uiEvents == null || lockedCapabilitiesWhileVisible == null)
                return;

            for (int i = 0; i < lockedCapabilitiesWhileVisible.Length; i++)
                uiEvents.OnGameplayCapabilityUnlockRequested?.Invoke(lockedCapabilitiesWhileVisible[i], CapabilityLockId);

            _capabilitiesLocked = false;
        }

        private void PauseGameplay()
        {
            if (!pauseGameplay || _gameplayPaused || Time.timeScale <= 0f)
                return;

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _gameplayPaused = true;
        }

        private void ReleasePause()
        {
            if (!_gameplayPaused)
                return;

            Time.timeScale = _previousTimeScale;
            _gameplayPaused = false;
        }

        private bool AreTutorialTipsEnabled()
        {
            return gameSession == null || gameSession.TutorialsEnabled;
        }

        private bool IsStepCompleted(string stepId)
        {
            return gameSession != null && gameSession.IsTutorialStepCompleted(stepId);
        }

        private void BindEvents()
        {
            if (_eventsBound || bowEvents == null)
                return;

            bowEvents.UnderdrawReleased += HandleUnderdrawReleased;
            bowEvents.OverdrawStarted += HandleOverdrawStarted;
            _eventsBound = true;
        }

        private void UnbindEvents()
        {
            if (!_eventsBound || bowEvents == null)
                return;

            bowEvents.UnderdrawReleased -= HandleUnderdrawReleased;
            bowEvents.OverdrawStarted -= HandleOverdrawStarted;
            _eventsBound = false;
        }

        private void ResolveDependencies()
        {
            if (uiDocument == null)
                TryGetComponent(out uiDocument);

            if (uiEvents == null)
                uiEvents = Resources.Load<UIEventsSO>(DefaultUiEventsResourcePath);

            if (gameSession == null)
                gameSession = GameSessionSO.LoadDefault();

            if (bowEvents == null)
                bowEvents = Resources.Load<PlayerBowEventsSO>(DefaultBowEventsResourcePath);
        }

        private static float ResolveDimension(float value, float fallback)
        {
            return float.IsNaN(value) || value <= 0f ? fallback : value;
        }
    }
}
