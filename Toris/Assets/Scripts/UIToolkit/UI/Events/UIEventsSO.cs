using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.Events;

namespace OutlandHaven.UIToolkit
{

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UIEventsSO")]
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

        public UnityAction<string> OnGameplayInputLockRequested;

        public UnityAction<string> OnGameplayInputUnlockRequested;

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
