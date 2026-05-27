using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.Events;

namespace OutlandHaven.UIToolkit
{
    // Named gameplay input channels that temporary flows can disable without blocking all input.
    // Keep the flow decisions outside InputManager; this event bus only describes the capability.
    public enum GameplayInputCapability
    {
        Movement = 0,
        Combat = 1,
        Interaction = 2,
        Inventory = 3,
        Skills = 4,
        QuestJournal = 5,
        PotionHotkeys = 6,
        QuickSaveLoad = 7
    }

    [CreateAssetMenu(menuName = "Outland Haven/UI/Events/UI Events")]
    public class UIEventsSO : ScriptableObject
    {
        // UI SFX requests live on the UI event bus so pure C# views can ask for sound
        // without hard-referencing AudioManager. UISfxEventBridge is the scene-side
        // listener that turns these IDs into actual AudioBootstrap playback.
        private const string DefaultButtonHoverSfxId = "ui_menu_hover";
        private const string DefaultButtonConfirmSfxId = "ui_menu_confirm";

        [Header("Default UI SFX")]
        [SerializeField] private string buttonHoverSfxId = DefaultButtonHoverSfxId;
        [SerializeField] private string buttonConfirmSfxId = DefaultButtonConfirmSfxId;
        [SerializeField, Min(0f)] private float buttonHoverCooldownSeconds = 0.04f;

        public UnityAction<ScreenType, object> OnRequestOpen; //for inventory, pass container with items' data as object

        public UnityAction<ScreenType> OnRequestClose;

        public UnityAction<ScreenType> OnScreenClose;

        public UnityAction OnRequestCloseAll;

        public UnityAction<ScreenType> OnScreenOpen;

        // Generic UI sound request channel. Keep gameplay-specific audio in its own
        // systems; this is for UI interactions such as buttons and screen lifecycle.
        public UnityAction<string> OnSfxRequested;

        // Broad lock for modal gameplay blockers such as story cards or death flow.
        public UnityAction<string> OnGameplayInputLockRequested;

        public UnityAction<string> OnGameplayInputUnlockRequested;

        // Fine-grained locks for guided flows. Each request carries an owner id so overlapping
        // systems can lock the same capability and release only their own request.
        public UnityAction<GameplayInputCapability, string> OnGameplayCapabilityLockRequested;

        public UnityAction<GameplayInputCapability, string> OnGameplayCapabilityUnlockRequested;

        public UnityAction<string> OnQuestJournalOpenRequested;

        public UnityAction OnQuickSaveRequested;

        public UnityAction OnQuickLoadRequested;

        // Death screen actions are UI intents only; gameplay consequences live in
        // DeathRespawnCoordinator.
        public UnityAction OnDeathPenaltySummaryRequested;

        public UnityAction<DeathPenaltySummary> OnDeathPenaltySummaryUpdated;

        public UnityAction OnDeathRespawnRequested;

        public UnityAction OnDeathMainMenuRequested;

        public UnityAction OnSystemInitializationComplete;

        public string ButtonHoverSfxId => string.IsNullOrWhiteSpace(buttonHoverSfxId)
            ? DefaultButtonHoverSfxId
            : buttonHoverSfxId;

        public string ButtonConfirmSfxId => string.IsNullOrWhiteSpace(buttonConfirmSfxId)
            ? DefaultButtonConfirmSfxId
            : buttonConfirmSfxId;

        public float ButtonHoverCooldownSeconds => Mathf.Max(0f, buttonHoverCooldownSeconds);

        public void RequestSfx(string sfxId)
        {
            if (!string.IsNullOrWhiteSpace(sfxId))
            {
                OnSfxRequested?.Invoke(sfxId);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(buttonHoverSfxId))
            {
                buttonHoverSfxId = DefaultButtonHoverSfxId;
            }

            if (string.IsNullOrWhiteSpace(buttonConfirmSfxId))
            {
                buttonConfirmSfxId = DefaultButtonConfirmSfxId;
            }
        }
#endif
    }
}
