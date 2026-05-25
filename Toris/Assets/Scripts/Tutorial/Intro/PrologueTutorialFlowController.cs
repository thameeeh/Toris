using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    [DefaultExecutionOrder(250)]
    public sealed class PrologueTutorialFlowController : MonoBehaviour
    {
        private const string DefaultUiEventsResourcePath = "GameData/SOForEvents/UI Events SO";
        private const string DefaultInputReaderResourcePath = "GameData/SOForEvents/InputReaderSO";
        private const string MovementCapabilityLockId = "PrologueTutorial.MovementPrompt";
        private const string PreWolfCapabilityLockId = "PrologueTutorial.PreWolf";
        private const string WolfEncounterCapabilityLockId = "PrologueTutorial.WolfEncounter";
        private const string MovementStepId = "prologue.movement";
        private const string ShootingStepId = "prologue.shooting";
        private const string DefaultPromptAnchorName = "TutorialPromptAnchor";
        private const float MovementInputThresholdSqr = 0.01f;
        private const float MinimumPromptVisibleSeconds = 0.2f;
        private const float PromptFallbackWidth = 180f;
        private const float PromptFallbackHeight = 46f;
        private const int FadeStepMilliseconds = 16;

        [Header("Flow")]
        [SerializeField] private PrologueStorySequenceController openingStorySequence;
        [SerializeField] private bool waitForOpeningStory = true;

        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIEventsSO uiEvents;
        [SerializeField] private GameSessionSO gameSession;
        [SerializeField] private PlayerInputReaderSO inputReader;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform promptAnchor;
        [SerializeField] private Camera worldCamera;

        [Header("Movement Prompt")]
        [SerializeField] private string movementPromptText = "WASD Move";
        [SerializeField] private Vector2 screenOffset = Vector2.zero;
        [SerializeField, Min(0f)] private float promptFadeSeconds = 0.25f;
        [SerializeField] private GameplayInputCapability[] lockedCapabilitiesDuringMovementPrompt =
        {
            GameplayInputCapability.Combat,
            GameplayInputCapability.Interaction,
            GameplayInputCapability.Inventory,
            GameplayInputCapability.Skills,
            GameplayInputCapability.QuestJournal,
            GameplayInputCapability.PotionHotkeys,
            GameplayInputCapability.QuickSaveLoad
        };

        [Header("Wolf Encounter Prompt")]
        [SerializeField] private bool keepCapabilitiesLockedUntilWolfEncounter = true;
        [SerializeField] private string shootingPromptText = "Hold LMB to shoot";
        [SerializeField] private GameplayInputCapability[] lockedCapabilitiesBeforeWolfEncounter =
        {
            GameplayInputCapability.Combat,
            GameplayInputCapability.Interaction,
            GameplayInputCapability.Inventory,
            GameplayInputCapability.Skills,
            GameplayInputCapability.QuestJournal,
            GameplayInputCapability.PotionHotkeys,
            GameplayInputCapability.QuickSaveLoad
        };
        [SerializeField] private GameplayInputCapability[] lockedCapabilitiesDuringWolfEncounter =
        {
            GameplayInputCapability.Interaction,
            GameplayInputCapability.Inventory,
            GameplayInputCapability.Skills,
            GameplayInputCapability.QuestJournal,
            GameplayInputCapability.PotionHotkeys,
            GameplayInputCapability.QuickSaveLoad
        };

        private VisualElement _promptRoot;
        private Label _promptLabel;
        private bool _movementPromptActive;
        private bool _shootingPromptActive;
        private bool _movementPromptCompleted;
        private bool _movementCapabilitiesLocked;
        private bool _preWolfCapabilitiesLocked;
        private bool _wolfEncounterCapabilitiesLocked;
        private bool _promptAnchorSearchCompleted;
        private float _movementPromptVisibleSince;
        private int _fadeVersion;
        private Enemy _encounterEnemy;

        private void Awake()
        {
            ResolveDependencies();
        }

        private void OnEnable()
        {
            ResolveDependencies();

            if (!AreTutorialTipsEnabled())
                return;

            if (waitForOpeningStory && openingStorySequence != null)
            {
                openingStorySequence.SequenceCompleted += HandleOpeningStoryCompleted;
                return;
            }

            if (ShouldRunMovementPrompt())
                BeginMovementPrompt();
            else if (keepCapabilitiesLockedUntilWolfEncounter)
                LockPreWolfEncounterCapabilities();
        }

        private void OnDisable()
        {
            if (openingStorySequence != null)
                openingStorySequence.SequenceCompleted -= HandleOpeningStoryCompleted;

            _movementPromptActive = false;
            _shootingPromptActive = false;
            UnbindShootingPromptInput();
            UnbindEncounterEnemy();
            ReleaseMovementPromptCapabilities();
            ReleasePreWolfEncounterCapabilities();
            ReleaseWolfEncounterCapabilities();
            HidePromptInstantly();
        }

        private void Update()
        {
            if (!_movementPromptActive && !_shootingPromptActive)
                return;

            UpdatePromptPosition();

            if (_movementPromptActive
                && inputReader != null
                && inputReader.Move.sqrMagnitude >= MovementInputThresholdSqr
                && Time.unscaledTime - _movementPromptVisibleSince >= MinimumPromptVisibleSeconds)
            {
                CompleteMovementPrompt();
            }
        }

        private void HandleOpeningStoryCompleted()
        {
            if (openingStorySequence != null)
                openingStorySequence.SequenceCompleted -= HandleOpeningStoryCompleted;

            if (ShouldRunMovementPrompt())
                BeginMovementPrompt();
            else if (keepCapabilitiesLockedUntilWolfEncounter)
                LockPreWolfEncounterCapabilities();
        }

        public void BeginWolfEncounterTutorial(Enemy encounterEnemy)
        {
            if (!AreTutorialTipsEnabled())
                return;

            if (_movementPromptActive)
            {
                _movementPromptActive = false;
                _movementPromptCompleted = true;
                ReleaseMovementPromptCapabilities();
                gameSession?.MarkTutorialStepCompleted(MovementStepId);
            }

            ReleasePreWolfEncounterCapabilities();
            if (encounterEnemy == null)
                return;

            LockWolfEncounterCapabilities();
            BindEncounterEnemy(encounterEnemy);

            if (!IsTutorialStepCompleted(ShootingStepId))
                BeginShootingPrompt();
        }

        private void BeginMovementPrompt()
        {
            if (_movementPromptActive || _movementPromptCompleted || !ShouldRunMovementPrompt())
                return;

            if (!EnsurePromptView())
                return;

            LockMovementPromptCapabilities();
            _movementPromptActive = true;
            _movementPromptVisibleSince = Time.unscaledTime;
            _promptLabel.text = string.IsNullOrWhiteSpace(movementPromptText)
                ? "WASD Move"
                : movementPromptText.Trim();
            _promptRoot.style.display = DisplayStyle.Flex;
            _promptRoot.BringToFront();
            UpdatePromptPosition();
            FadePromptTo(1f, promptFadeSeconds, null);
        }

        private void CompleteMovementPrompt()
        {
            if (!_movementPromptActive)
                return;

            _movementPromptActive = false;
            _movementPromptCompleted = true;
            ReleaseMovementPromptCapabilities();
            gameSession?.MarkTutorialStepCompleted(MovementStepId);
            if (keepCapabilitiesLockedUntilWolfEncounter)
                LockPreWolfEncounterCapabilities();

            FadePromptTo(0f, promptFadeSeconds, HidePromptInstantly);
        }

        private void BeginShootingPrompt()
        {
            if (_shootingPromptActive || !EnsurePromptView())
                return;

            _shootingPromptActive = true;
            _promptLabel.text = string.IsNullOrWhiteSpace(shootingPromptText)
                ? "Hold LMB to shoot"
                : shootingPromptText.Trim();
            _promptRoot.style.display = DisplayStyle.Flex;
            _promptRoot.BringToFront();
            UpdatePromptPosition();
            BindShootingPromptInput();
            FadePromptTo(1f, promptFadeSeconds, null);
        }

        private void CompleteShootingPrompt()
        {
            if (!_shootingPromptActive)
                return;

            _shootingPromptActive = false;
            UnbindShootingPromptInput();
            gameSession?.MarkTutorialStepCompleted(ShootingStepId);
            FadePromptTo(0f, promptFadeSeconds, HidePromptInstantly);
        }

        private void HandleShootStarted()
        {
            CompleteShootingPrompt();
        }

        private void HandleEncounterEnemyDied(Enemy enemy)
        {
            _shootingPromptActive = false;
            UnbindShootingPromptInput();
            UnbindEncounterEnemy();
            ReleaseWolfEncounterCapabilities();
            HidePromptInstantly();
        }

        private void LockMovementPromptCapabilities()
        {
            LockCapabilities(lockedCapabilitiesDuringMovementPrompt, MovementCapabilityLockId, ref _movementCapabilitiesLocked);
        }

        private void ReleaseMovementPromptCapabilities()
        {
            ReleaseCapabilities(lockedCapabilitiesDuringMovementPrompt, MovementCapabilityLockId, ref _movementCapabilitiesLocked);
        }

        private void LockPreWolfEncounterCapabilities()
        {
            LockCapabilities(lockedCapabilitiesBeforeWolfEncounter, PreWolfCapabilityLockId, ref _preWolfCapabilitiesLocked);
        }

        private void ReleasePreWolfEncounterCapabilities()
        {
            ReleaseCapabilities(lockedCapabilitiesBeforeWolfEncounter, PreWolfCapabilityLockId, ref _preWolfCapabilitiesLocked);
        }

        private void LockWolfEncounterCapabilities()
        {
            LockCapabilities(lockedCapabilitiesDuringWolfEncounter, WolfEncounterCapabilityLockId, ref _wolfEncounterCapabilitiesLocked);
        }

        private void ReleaseWolfEncounterCapabilities()
        {
            ReleaseCapabilities(lockedCapabilitiesDuringWolfEncounter, WolfEncounterCapabilityLockId, ref _wolfEncounterCapabilitiesLocked);
        }

        private void LockCapabilities(GameplayInputCapability[] capabilities, string lockId, ref bool lockState)
        {
            if (lockState || uiEvents == null || capabilities == null)
                return;

            for (int i = 0; i < capabilities.Length; i++)
                uiEvents.OnGameplayCapabilityLockRequested?.Invoke(capabilities[i], lockId);

            lockState = true;
        }

        private void ReleaseCapabilities(GameplayInputCapability[] capabilities, string lockId, ref bool lockState)
        {
            if (!lockState || uiEvents == null || capabilities == null)
                return;

            for (int i = 0; i < capabilities.Length; i++)
                uiEvents.OnGameplayCapabilityUnlockRequested?.Invoke(capabilities[i], lockId);

            lockState = false;
        }

        private void BindShootingPromptInput()
        {
            if (inputReader != null)
                inputReader.OnShootStarted += HandleShootStarted;
        }

        private void UnbindShootingPromptInput()
        {
            if (inputReader != null)
                inputReader.OnShootStarted -= HandleShootStarted;
        }

        private void BindEncounterEnemy(Enemy encounterEnemy)
        {
            if (_encounterEnemy == encounterEnemy)
                return;

            UnbindEncounterEnemy();
            _encounterEnemy = encounterEnemy;
            if (_encounterEnemy != null)
                _encounterEnemy.Died += HandleEncounterEnemyDied;
        }

        private void UnbindEncounterEnemy()
        {
            if (_encounterEnemy != null)
                _encounterEnemy.Died -= HandleEncounterEnemyDied;

            _encounterEnemy = null;
        }

        private bool AreTutorialTipsEnabled()
        {
            return gameSession == null || gameSession.TutorialsEnabled;
        }

        private bool ShouldRunMovementPrompt()
        {
            return AreTutorialTipsEnabled() && !IsTutorialStepCompleted(MovementStepId);
        }

        private bool IsTutorialStepCompleted(string stepId)
        {
            return gameSession != null && gameSession.IsTutorialStepCompleted(stepId);
        }

        private bool EnsurePromptView()
        {
            if (_promptRoot != null)
                return true;

            VisualElement host = ResolveHost();
            if (host == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[PrologueTutorialFlowController] No UI host was available for the movement prompt.", this);
#endif
                return false;
            }

            _promptRoot = new VisualElement { name = "PrologueMovementPrompt" };
            _promptRoot.AddToClassList("prologue-tutorial-prompt");
            _promptRoot.pickingMode = PickingMode.Ignore;

            _promptLabel = new Label { name = "PrologueMovementPromptLabel" };
            _promptLabel.AddToClassList("prologue-tutorial-prompt__label");
            _promptRoot.Add(_promptLabel);

            host.Add(_promptRoot);
            HidePromptInstantly();
            return true;
        }

        private void HidePromptInstantly()
        {
            _fadeVersion++;

            if (_promptRoot == null)
                return;

            _promptRoot.style.opacity = 0f;
            _promptRoot.style.display = DisplayStyle.None;
        }

        private void FadePromptTo(float targetOpacity, float durationSeconds, System.Action completed)
        {
            if (_promptRoot == null)
            {
                completed?.Invoke();
                return;
            }

            int fadeVersion = ++_fadeVersion;
            float startOpacity = _promptRoot.resolvedStyle.opacity;
            float startTime = Time.realtimeSinceStartup;
            float duration = Mathf.Max(0.001f, durationSeconds);

            _promptRoot.style.display = DisplayStyle.Flex;
            _promptRoot.schedule.Execute(() => TickPromptFade(startOpacity, targetOpacity, startTime, duration, fadeVersion, completed))
                .ExecuteLater(FadeStepMilliseconds);
        }

        private void TickPromptFade(
            float startOpacity,
            float targetOpacity,
            float startTime,
            float duration,
            int fadeVersion,
            System.Action completed)
        {
            if (_promptRoot == null || fadeVersion != _fadeVersion)
                return;

            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            _promptRoot.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, eased);

            if (t >= 1f)
            {
                completed?.Invoke();
                return;
            }

            _promptRoot.schedule.Execute(() => TickPromptFade(startOpacity, targetOpacity, startTime, duration, fadeVersion, completed))
                .ExecuteLater(FadeStepMilliseconds);
        }

        private void UpdatePromptPosition()
        {
            if (_promptRoot == null)
                return;

            ResolvePlayerTarget();
            ResolvePromptAnchor();
            ResolveCamera();

            Transform anchor = promptAnchor != null ? promptAnchor : playerTarget;
            if (anchor == null || worldCamera == null)
                return;

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(anchor.position);
            if (screenPoint.z < 0f)
                return;

            VisualElement host = _promptRoot.parent;
            float hostWidth = ResolveDimension(host?.resolvedStyle.width ?? 0f, Screen.width);
            float hostHeight = ResolveDimension(host?.resolvedStyle.height ?? 0f, Screen.height);
            float promptWidth = ResolveDimension(_promptRoot.resolvedStyle.width, PromptFallbackWidth);
            float promptHeight = ResolveDimension(_promptRoot.resolvedStyle.height, PromptFallbackHeight);

            float left = screenPoint.x - promptWidth * 0.5f + screenOffset.x;
            float top = hostHeight - screenPoint.y - promptHeight * 0.5f + screenOffset.y;

            _promptRoot.style.left = Mathf.Clamp(left, 0f, Mathf.Max(0f, hostWidth - promptWidth));
            _promptRoot.style.top = Mathf.Clamp(top, 0f, Mathf.Max(0f, hostHeight - promptHeight));
        }

        private VisualElement ResolveHost()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
            {
                UIManager manager = FindFirstObjectByType<UIManager>();
                if (manager != null)
                    manager.TryGetComponent(out uiDocument);
            }

            return uiDocument != null ? uiDocument.rootVisualElement : null;
        }

        private void ResolveDependencies()
        {
            if (openingStorySequence == null)
                TryGetComponent(out openingStorySequence);

            if (uiEvents == null)
                uiEvents = Resources.Load<UIEventsSO>(DefaultUiEventsResourcePath);

            if (gameSession == null)
                gameSession = GameSessionSO.LoadDefault();

            if (inputReader == null)
                inputReader = Resources.Load<PlayerInputReaderSO>(DefaultInputReaderResourcePath);

            ResolvePlayerTarget();
            ResolvePromptAnchor();
            ResolveCamera();
        }

        private void ResolvePlayerTarget()
        {
            if (playerTarget != null)
                return;

            PlayerController player = FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerTarget = player.transform;
                _promptAnchorSearchCompleted = false;
            }
        }

        private void ResolvePromptAnchor()
        {
            if (promptAnchor != null || playerTarget == null || _promptAnchorSearchCompleted)
                return;

            Transform directAnchor = playerTarget.Find(DefaultPromptAnchorName);
            if (directAnchor != null)
            {
                promptAnchor = directAnchor;
                _promptAnchorSearchCompleted = true;
                return;
            }

            Transform[] childTransforms = playerTarget.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < childTransforms.Length; i++)
            {
                Transform childTransform = childTransforms[i];
                if (childTransform != playerTarget && childTransform.name == DefaultPromptAnchorName)
                {
                    promptAnchor = childTransform;
                    break;
                }
            }

            _promptAnchorSearchCompleted = true;
        }

        private void ResolveCamera()
        {
            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        private static float ResolveDimension(float value, float fallback)
        {
            return float.IsNaN(value) || value <= 0f ? fallback : value;
        }
    }
}
