using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    [DefaultExecutionOrder(250)]
    public sealed class PrologueTutorialFlowController : MonoBehaviour
    {
        private const string DefaultUiEventsResourcePath = "GameData/SOForEvents/UI Events SO";
        private const string DefaultUiInventoryEventsResourcePath = "GameData/SOForEvents/UI Inventory Events SO";
        private const string DefaultInputReaderResourcePath = "GameData/SOForEvents/InputReaderSO";
        private const string MovementCapabilityLockId = "PrologueTutorial.MovementPrompt";
        private const string PreWolfCapabilityLockId = "PrologueTutorial.PreWolf";
        private const string WolfEncounterCapabilityLockId = "PrologueTutorial.WolfEncounter";
        private const string PickupLessonCapabilityLockId = "PrologueTutorial.PickupLesson";
        private const string HudLessonCapabilityLockId = "PrologueTutorial.HudLesson";
        private const string MovementStepId = "prologue.movement";
        private const string ShootingStepId = "prologue.shooting";
        private const string RewardsStepId = "prologue.rewards";
        private const string PickupStepId = "prologue.pickup";
        private const string HudMenuStepId = "prologue.hud.menu";
        private const string InventoryOpenStepId = "prologue.inventory.open";
        private const string EquipTrainingBowStepId = "prologue.inventory.equip_training_bow";
        private const string StatsToggleStepId = "prologue.inventory.stats_toggle";
        private const string StatsPanelStepId = "prologue.inventory.stats_panel";
        private const string PotionSlotsStepId = "prologue.inventory.potion_slots";
        private const string PotionAssignStepId = "prologue.inventory.assign_potion";
        private const string HudPotionHotkeysStepId = "prologue.hud.potion_hotkeys";
        private const string HudInventoryButtonAnchorId = "hud.inventory_button";
        private const string InventoryTrainingBowAnchorId = "inventory.item.training_bow";
        private const string DefaultPromptAnchorName = "TutorialPromptAnchor";
        private const float MovementInputThresholdSqr = 0.01f;
        private const float MinimumPromptVisibleSeconds = 0.2f;
        private const float PromptFallbackWidth = 180f;
        private const float PromptFallbackHeight = 46f;
        private const int FadeStepMilliseconds = 16;
        private const int MaxHudAnchorResolveAttempts = 8;
        private const int HudAnchorResolveRetryMilliseconds = 50;
        private const int StatsDrawerRevealDelayMilliseconds = 170;

        private enum PostWolfHudLesson
        {
            None = 0,
            Rewards = 1,
            MenuToggle = 2,
            Inventory = 3,
            EquipTrainingBow = 4,
            StatsToggle = 5,
            StatsPanel = 6,
            PotionSlots = 7,
            AssignPotion = 8,
            HudPotionHotkeys = 9
        }

        [Header("Flow")]
        [SerializeField] private PrologueStorySequenceController openingStorySequence;
        [SerializeField] private bool waitForOpeningStory = true;

        [Header("References")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIEventsSO uiEvents;
        [SerializeField] private UIInventoryEventsSO uiInventoryEvents;
        [SerializeField] private GameSessionSO gameSession;
        [SerializeField] private TutorialCatalogSO tutorialCatalog;
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

        [Header("Post-Wolf Loot Lesson")]
        [SerializeField] private InventoryItemSO tutorialBowItem;
        [SerializeField] private InventoryItemSO tutorialPotionItem;
        [SerializeField] private string pickupPromptText = "E Pick Up";
        [SerializeField] private GameplayInputCapability[] lockedCapabilitiesDuringPickupLesson =
        {
            GameplayInputCapability.Inventory,
            GameplayInputCapability.Skills,
            GameplayInputCapability.QuestJournal,
            GameplayInputCapability.PotionHotkeys,
            GameplayInputCapability.QuickSaveLoad
        };
        [SerializeField] private GameplayInputCapability[] lockedCapabilitiesDuringHudLesson =
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
        private TutorialOverlayView _hudLessonOverlay;
        private VisualElement _hudLessonClickAnchor;
        private bool _movementPromptActive;
        private bool _shootingPromptActive;
        private bool _pickupPromptActive;
        private bool _movementPromptCompleted;
        private bool _movementCapabilitiesLocked;
        private bool _preWolfCapabilitiesLocked;
        private bool _wolfEncounterCapabilitiesLocked;
        private bool _pickupLessonCapabilitiesLocked;
        private bool _hudLessonCapabilitiesLocked;
        private bool _postWolfLessonEventsBound;
        private bool _hudLessonPausedGameplay;
        private bool _promptAnchorSearchCompleted;
        private float _movementPromptVisibleSince;
        private float _timeScaleBeforeHudLesson = 1f;
        private int _fadeVersion;
        private int _hudLessonVersion;
        private PostWolfHudLesson _activeHudLesson;
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
            _pickupPromptActive = false;
            UnbindShootingPromptInput();
            UnbindEncounterEnemy();
            UnbindPostWolfLessonEvents();
            ReleaseMovementPromptCapabilities();
            ReleasePreWolfEncounterCapabilities();
            ReleaseWolfEncounterCapabilities();
            ReleasePickupLessonCapabilities();
            HideHudLesson();
            HidePromptInstantly();
        }

        private void Update()
        {
            if (!_movementPromptActive
                && !_shootingPromptActive
                && !_pickupPromptActive)
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

            if (AreTutorialTipsEnabled())
                BeginPostWolfRewardsLesson();
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

        private void LockPickupLessonCapabilities()
        {
            LockCapabilities(lockedCapabilitiesDuringPickupLesson, PickupLessonCapabilityLockId, ref _pickupLessonCapabilitiesLocked);
        }

        private void ReleasePickupLessonCapabilities()
        {
            ReleaseCapabilities(lockedCapabilitiesDuringPickupLesson, PickupLessonCapabilityLockId, ref _pickupLessonCapabilitiesLocked);
        }

        private void LockHudLessonCapabilities()
        {
            LockCapabilities(lockedCapabilitiesDuringHudLesson, HudLessonCapabilityLockId, ref _hudLessonCapabilitiesLocked);
        }

        private void ReleaseHudLessonCapabilities()
        {
            ReleaseCapabilities(lockedCapabilitiesDuringHudLesson, HudLessonCapabilityLockId, ref _hudLessonCapabilitiesLocked);
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

        private void BeginPostWolfRewardsLesson()
        {
            if (IsPostWolfInventoryLessonCompleted())
                return;

            BindPostWolfLessonEvents();

            if (IsTutorialStepCompleted(RewardsStepId))
            {
                BeginPostWolfPickupLesson();
                return;
            }

            if (!BeginHudLesson(RewardsStepId, PostWolfHudLesson.Rewards))
                BeginPostWolfPickupLesson();
        }

        private void BeginPostWolfPickupLesson()
        {
            if (IsPostWolfInventoryLessonCompleted())
                return;

            BindPostWolfLessonEvents();

            if (IsTutorialStepCompleted(PickupStepId) || HasTutorialLootInBackpack())
            {
                CompletePickupLesson();
                return;
            }

            LockPickupLessonCapabilities();
            ShowPickupPrompt();
        }

        private void ShowPickupPrompt()
        {
            _pickupPromptActive = true;
            ShowInstructionPrompt(pickupPromptText, "E Pick Up");
        }

        private void HandlePostWolfInventoryUpdated()
        {
            if (_pickupPromptActive && HasTutorialLootInBackpack())
            {
                CompletePickupLesson();
                return;
            }

            if (_activeHudLesson == PostWolfHudLesson.EquipTrainingBow && IsTutorialBowEquipped())
            {
                CompleteTrainingBowLesson();
                return;
            }

            if (_activeHudLesson == PostWolfHudLesson.AssignPotion
                && (IsTutorialPotionAssigned() || IsTutorialPotionUnavailableForAssignment()))
            {
                CompletePotionAssignmentLesson();
            }
        }

        private void CompletePickupLesson()
        {
            _pickupPromptActive = false;
            ReleasePickupLessonCapabilities();
            gameSession?.MarkTutorialStepCompleted(PickupStepId);
            HidePromptInstantly();

            if (IsPostWolfInventoryLessonCompleted())
            {
                UnbindPostWolfLessonEvents();
                return;
            }

            if (IsTrainingBowAlreadyVisibleInInventory())
            {
                gameSession?.MarkTutorialStepCompleted(HudMenuStepId);
                gameSession?.MarkTutorialStepCompleted(InventoryOpenStepId);
                BeginPostWolfEquipTrainingBowLesson();
                return;
            }

            if (IsHudInventoryButtonAlreadyVisible())
            {
                gameSession?.MarkTutorialStepCompleted(HudMenuStepId);
                BeginPostWolfInventoryOpenLesson();
                return;
            }

            if (!BeginHudLesson(HudMenuStepId, PostWolfHudLesson.MenuToggle))
                UnbindPostWolfLessonEvents();
        }

        private bool IsHudInventoryButtonAlreadyVisible()
        {
            // Cross-system boundary: the tutorial only reads the HUD's registered anchor.
            // A visible Inventory action means the player already opened the HUD menu naturally.
            return TutorialAnchorRegistry.TryGetVisibleBounds(HudInventoryButtonAnchorId, out _);
        }

        private bool IsTrainingBowAlreadyVisibleInInventory()
        {
            // Cross-system boundary: visible item anchors communicate existing UI progress
            // without the tutorial opening, closing, or inspecting the Inventory view directly.
            return TutorialAnchorRegistry.TryGetVisibleBounds(InventoryTrainingBowAnchorId, out _);
        }

        private void BeginPostWolfInventoryOpenLesson()
        {
            if (!BeginHudLesson(InventoryOpenStepId, PostWolfHudLesson.Inventory))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private void HandlePostWolfScreenOpened(ScreenType screenType)
        {
            if (_activeHudLesson != PostWolfHudLesson.Inventory || screenType != ScreenType.Inventory)
                return;

            gameSession?.MarkTutorialStepCompleted(InventoryOpenStepId);

            BeginPostWolfEquipTrainingBowLesson();
        }

        private void BeginPostWolfEquipTrainingBowLesson()
        {
            if (IsTutorialBowEquipped())
            {
                CompleteTrainingBowLesson();
                return;
            }

            if (IsTutorialStepCompleted(EquipTrainingBowStepId))
            {
                BeginPostWolfStatsToggleLesson();
                return;
            }

            if (!BeginHudLesson(EquipTrainingBowStepId, PostWolfHudLesson.EquipTrainingBow))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private bool IsTutorialBowEquipped()
        {
            return tutorialBowItem != null
                && ContainsItem(gameSession != null ? gameSession.PlayerEquipment : null, tutorialBowItem);
        }

        private void CompleteTrainingBowLesson()
        {
            if (!IsTutorialBowEquipped())
                return;

            gameSession?.MarkTutorialStepCompleted(EquipTrainingBowStepId);
            HideHudLesson();
            BeginPostWolfStatsToggleLesson();
        }

        private void BeginPostWolfStatsToggleLesson()
        {
            if (IsPostWolfInventoryLessonCompleted())
            {
                CompletePostWolfInventoryLesson();
                return;
            }

            if (IsTutorialStepCompleted(StatsToggleStepId))
            {
                BeginPostWolfStatsPanelLesson();
                return;
            }

            if (!BeginHudLesson(StatsToggleStepId, PostWolfHudLesson.StatsToggle))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private void SchedulePostWolfStatsPanelLesson()
        {
            VisualElement host = ResolveHost();
            if (host == null)
            {
                BeginPostWolfStatsPanelLesson();
                return;
            }

            host.schedule.Execute(() => BeginPostWolfStatsPanelLesson())
                .ExecuteLater(StatsDrawerRevealDelayMilliseconds);
        }

        private void BeginPostWolfStatsPanelLesson()
        {
            if (IsTutorialStepCompleted(StatsPanelStepId))
            {
                BeginPostWolfPotionSlotsLesson();
                return;
            }

            if (!BeginHudLesson(StatsPanelStepId, PostWolfHudLesson.StatsPanel))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private void BeginPostWolfPotionSlotsLesson()
        {
            if (IsTutorialStepCompleted(PotionSlotsStepId))
            {
                BeginPostWolfPotionAssignmentLesson();
                return;
            }

            if (!BeginHudLesson(PotionSlotsStepId, PostWolfHudLesson.PotionSlots))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private void BeginPostWolfPotionAssignmentLesson()
        {
            if (IsTutorialPotionAssigned()
                || IsTutorialPotionUnavailableForAssignment()
                || IsTutorialStepCompleted(PotionAssignStepId))
            {
                CompletePotionAssignmentLesson();
                return;
            }

            if (!BeginHudLesson(PotionAssignStepId, PostWolfHudLesson.AssignPotion))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private void CompletePotionAssignmentLesson()
        {
            gameSession?.MarkTutorialStepCompleted(PotionAssignStepId);
            HideHudLesson();
            BeginPostWolfHudPotionHotkeysLesson();
        }

        private void BeginPostWolfHudPotionHotkeysLesson()
        {
            if (IsTutorialStepCompleted(HudPotionHotkeysStepId))
            {
                CompletePostWolfInventoryLesson();
                return;
            }

            if (!BeginHudLesson(HudPotionHotkeysStepId, PostWolfHudLesson.HudPotionHotkeys))
            {
                HideHudLesson();
                UnbindPostWolfLessonEvents();
            }
        }

        private void CompletePostWolfInventoryLesson()
        {
            HideHudLesson();
            UnbindPostWolfLessonEvents();
        }

        private bool IsPostWolfInventoryLessonCompleted()
        {
            return IsTutorialStepCompleted(HudPotionHotkeysStepId);
        }

        private bool IsTutorialPotionAssigned()
        {
            // Cross-system boundary: the tutorial observes the authoritative potion
            // inventory after InventoryTransferManagerSO handles the drag/drop.
            return tutorialPotionItem != null
                && ContainsItem(gameSession != null ? gameSession.PlayerPotionInventory : null, tutorialPotionItem);
        }

        private bool IsTutorialPotionUnavailableForAssignment()
        {
            return tutorialPotionItem != null
                && !ContainsItem(gameSession != null ? gameSession.PlayerInventory : null, tutorialPotionItem)
                && !ContainsItem(gameSession != null ? gameSession.PlayerPotionInventory : null, tutorialPotionItem);
        }

        private bool BeginHudLesson(string stepId, PostWolfHudLesson lesson)
        {
            if (tutorialCatalog == null
                || !tutorialCatalog.TryGetStep(stepId, out TutorialStepDefinition step)
                || !EnsureHudLessonOverlay())
            {
                return false;
            }

            _hudLessonVersion++;
            _activeHudLesson = lesson;
            UnbindHudLessonClickAnchor();

            if (step.BlocksInput)
                LockHudLessonCapabilities();
            else
                ReleaseHudLessonCapabilities();

            if (step.PauseGameplay)
                PauseGameplayForHudLesson();
            else
                ReleaseHudLessonPause();

            TryShowHudLesson(step, _hudLessonVersion, 0);
            return true;
        }

        private void TryShowHudLesson(TutorialStepDefinition step, int version, int attempt)
        {
            if (version != _hudLessonVersion || _activeHudLesson == PostWolfHudLesson.None)
                return;

            if (TutorialAnchorRegistry.TryGetVisibleBounds(step.AnchorId, out Rect anchorBounds))
            {
                if ((_activeHudLesson == PostWolfHudLesson.MenuToggle
                    || _activeHudLesson == PostWolfHudLesson.StatsToggle)
                    && step.AllowHighlightedClick
                    && step.DismissMode == TutorialDismissMode.ClickHighlighted)
                {
                    BindHudLessonClickAnchor(step.AnchorId);
                }

                _hudLessonOverlay.Show(step, anchorBounds, hasNextStep: false);
                return;
            }

            VisualElement host = ResolveHost();
            if (attempt < MaxHudAnchorResolveAttempts && host != null)
            {
                host.schedule.Execute(() => TryShowHudLesson(step, version, attempt + 1))
                    .ExecuteLater(HudAnchorResolveRetryMilliseconds);
                return;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[PrologueTutorialFlowController] Could not resolve HUD tutorial anchor '{step.AnchorId}'.", this);
#endif
            bool wasRewardsLesson = _activeHudLesson == PostWolfHudLesson.Rewards;
            HideHudLesson();

            if (wasRewardsLesson)
                BeginPostWolfPickupLesson();
            else
                UnbindPostWolfLessonEvents();
        }

        private bool EnsureHudLessonOverlay()
        {
            if (_hudLessonOverlay != null)
                return true;

            VisualElement host = ResolveHost();
            if (host == null)
                return false;

            _hudLessonOverlay = new TutorialOverlayView(host);
            _hudLessonOverlay.DismissRequested += HandleHudLessonDismissRequested;
            return true;
        }

        private void HandleHudLessonDismissRequested()
        {
            switch (_activeHudLesson)
            {
                case PostWolfHudLesson.Rewards:
                    gameSession?.MarkTutorialStepCompleted(RewardsStepId);
                    HideHudLesson();
                    BeginPostWolfPickupLesson();
                    break;
                case PostWolfHudLesson.StatsPanel:
                    gameSession?.MarkTutorialStepCompleted(StatsPanelStepId);
                    HideHudLesson();
                    BeginPostWolfPotionSlotsLesson();
                    break;
                case PostWolfHudLesson.PotionSlots:
                    gameSession?.MarkTutorialStepCompleted(PotionSlotsStepId);
                    HideHudLesson();
                    BeginPostWolfPotionAssignmentLesson();
                    break;
                case PostWolfHudLesson.HudPotionHotkeys:
                    gameSession?.MarkTutorialStepCompleted(HudPotionHotkeysStepId);
                    CompletePostWolfInventoryLesson();
                    break;
            }
        }

        private void BindHudLessonClickAnchor(string anchorId)
        {
            UnbindHudLessonClickAnchor();

            if (!TutorialAnchorRegistry.TryGetElement(anchorId, out _hudLessonClickAnchor))
                return;

            _hudLessonClickAnchor.RegisterCallback<ClickEvent>(HandleHudLessonAnchorClicked);
        }

        private void UnbindHudLessonClickAnchor()
        {
            if (_hudLessonClickAnchor == null)
                return;

            _hudLessonClickAnchor.UnregisterCallback<ClickEvent>(HandleHudLessonAnchorClicked);
            _hudLessonClickAnchor = null;
        }

        private void HandleHudLessonAnchorClicked(ClickEvent evt)
        {
            switch (_activeHudLesson)
            {
                case PostWolfHudLesson.MenuToggle:
                    gameSession?.MarkTutorialStepCompleted(HudMenuStepId);
                    _hudLessonOverlay?.Hide();
                    UnbindHudLessonClickAnchor();
                    BeginPostWolfInventoryOpenLesson();
                    break;
                case PostWolfHudLesson.StatsToggle:
                    gameSession?.MarkTutorialStepCompleted(StatsToggleStepId);
                    _hudLessonOverlay?.Hide();
                    UnbindHudLessonClickAnchor();
                    SchedulePostWolfStatsPanelLesson();
                    break;
            }
        }

        private void HideHudLesson()
        {
            _hudLessonVersion++;
            _activeHudLesson = PostWolfHudLesson.None;
            UnbindHudLessonClickAnchor();
            _hudLessonOverlay?.Hide();
            ReleaseHudLessonCapabilities();
            ReleaseHudLessonPause();
        }

        private void PauseGameplayForHudLesson()
        {
            if (_hudLessonPausedGameplay || Time.timeScale <= 0f)
                return;

            _timeScaleBeforeHudLesson = Time.timeScale;
            Time.timeScale = 0f;
            _hudLessonPausedGameplay = true;
        }

        private void ReleaseHudLessonPause()
        {
            if (!_hudLessonPausedGameplay)
                return;

            Time.timeScale = _timeScaleBeforeHudLesson;
            _hudLessonPausedGameplay = false;
        }

        private void BindPostWolfLessonEvents()
        {
            if (_postWolfLessonEventsBound)
                return;

            if (uiInventoryEvents != null)
                uiInventoryEvents.OnInventoryUpdated += HandlePostWolfInventoryUpdated;

            if (uiEvents != null)
                uiEvents.OnScreenOpen += HandlePostWolfScreenOpened;

            _postWolfLessonEventsBound = true;
        }

        private void UnbindPostWolfLessonEvents()
        {
            if (!_postWolfLessonEventsBound)
                return;

            if (uiInventoryEvents != null)
                uiInventoryEvents.OnInventoryUpdated -= HandlePostWolfInventoryUpdated;

            if (uiEvents != null)
                uiEvents.OnScreenOpen -= HandlePostWolfScreenOpened;

            _postWolfLessonEventsBound = false;
        }

        private bool HasTutorialLootInBackpack()
        {
            InventoryManager playerInventory = gameSession != null ? gameSession.PlayerInventory : null;
            if (playerInventory == null)
                return false;

            if (tutorialBowItem == null && tutorialPotionItem == null)
                return true;

            return ContainsItem(playerInventory, tutorialBowItem)
                && ContainsItem(playerInventory, tutorialPotionItem);
        }

        private static bool ContainsItem(InventoryManager inventory, InventoryItemSO requiredItem)
        {
            if (requiredItem == null)
                return true;

            if (inventory == null || inventory.LiveSlots == null)
                return false;

            for (int i = 0; i < inventory.LiveSlots.Count; i++)
            {
                InventorySlot slot = inventory.LiveSlots[i];
                if (slot != null
                    && !slot.IsEmpty
                    && slot.HeldItem?.BaseItem == requiredItem
                    && slot.Count > 0)
                {
                    return true;
                }
            }

            return false;
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

        private bool ShowInstructionPrompt(string configuredText, string fallbackText)
        {
            if (!EnsurePromptView())
                return false;

            _promptLabel.text = string.IsNullOrWhiteSpace(configuredText) ? fallbackText : configuredText.Trim();
            _promptRoot.style.display = DisplayStyle.Flex;
            _promptRoot.BringToFront();
            UpdatePromptPosition();
            FadePromptTo(1f, promptFadeSeconds, null);
            return true;
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

            if (uiInventoryEvents == null)
                uiInventoryEvents = Resources.Load<UIInventoryEventsSO>(DefaultUiInventoryEventsResourcePath);

            if (gameSession == null)
                gameSession = GameSessionSO.LoadDefault();

            if (tutorialCatalog == null)
                tutorialCatalog = TutorialCatalogSO.LoadDefault();

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
