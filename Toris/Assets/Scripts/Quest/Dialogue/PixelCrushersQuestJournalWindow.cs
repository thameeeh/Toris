using System;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using PixelCrushers;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Toris extension of Pixel Crushers' Standard UI quest log.
/// Adds an Available Jobs view that displays Pixel Crushers quests in the Grantable state.
/// </summary>
[AddComponentMenu("Toris/Quest/Pixel Crushers Quest Journal Window")]
public class PixelCrushersQuestJournalWindow : StandardUIQuestLogWindow
{
    private const QuestState AvailableQuestStateMask = QuestState.Grantable;

    [Header("Available Jobs")]
    [Tooltip("Optional heading shown when the journal is displaying available jobs.")]
    [SerializeField] private UITextField _showingAvailableJobsHeading;
    [Tooltip("Optional button that switches the journal to available jobs.")]
    [SerializeField] private Button _availableJobsButton;
    [Tooltip("Text shown when there are no grantable jobs.")]
    [SerializeField] private string _noAvailableJobsText = "No Available Jobs";
    [Tooltip("Use active quest button styling for available jobs until dedicated templates exist.")]
    [SerializeField] private bool _useActiveQuestTemplatesForAvailableJobs = true;
    [Tooltip("Create an Available Jobs button beside Pixel Crushers' Active/Completed buttons if one is not assigned.")]
    [SerializeField] private bool _createAvailableJobsButtonIfMissing = true;
    [Tooltip("Label used for the generated Available Jobs button and heading fallback.")]
    [SerializeField] private string _availableJobsButtonText = "Available Jobs";
    [Tooltip("Label used for the details-panel button that accepts a selected available job.")]
    [SerializeField] private string _acceptAvailableJobButtonText = "Accept Job";
    [Tooltip("Quest entry number to activate when accepting a grantable quest.")]
    [SerializeField] private int _acceptedQuestEntryNumber = 1;
    [Tooltip("When enabled, a grantable quest cannot be accepted if another active quest has the same Pixel Crushers Group.")]
    [SerializeField] private bool _enforceOneActiveQuestPerGroup = true;
    [Tooltip("Label shown when another quest from the same source/group is already active.")]
    [SerializeField] private string _sourceBlockedJobButtonText = "Finish Current Job First";
    [Header("Gameplay Input Lock")]
    [Tooltip("Project UI event channel used to freeze Toris gameplay input while this Pixel Crushers quest journal is open.")]
    [SerializeField] private UIEventsSO _uiEvents;
    [Tooltip("Named gameplay input lock used while the quest journal is open.")]
    [SerializeField] private string _gameplayInputLockId = "PixelCrushersQuestJournal";

    private bool _availableJobsButtonBound;
    private bool _gameplayInputLocked;
    private string _availableJobsGroupFilter = string.Empty;

    public bool IsShowingAvailableJobs => currentQuestStateMask == AvailableQuestStateMask;

    public override void Awake()
    {
        base.Awake();
        EnsureAvailableJobsButton();
        BindAvailableJobsButton();
    }

    public override void OnQuestListUpdated()
    {
        base.OnQuestListUpdated();
        UpdateAvailableJobsViewState();
    }

    public override void OpenWindow(Action openedWindowHandler)
    {
        base.OpenWindow(openedWindowHandler);
        RequestGameplayInputLock();
    }

    public override void CloseWindow(Action closedWindowHandler)
    {
        base.CloseWindow(closedWindowHandler);
        ReleaseGameplayInputLock();
    }

    protected override void OnDisable()
    {
        ReleaseGameplayInputLock();
        base.OnDisable();
    }

    public virtual void ClickShowAvailableJobs(object data)
    {
        OpenAvailableJobs(data as string);
    }

    public virtual void ClickShowAvailableJobsButton()
    {
        ClickShowAvailableJobs(null);
    }

    public virtual void OpenAvailableJobs()
    {
        OpenAvailableJobs(string.Empty);
    }

    public virtual void OpenAvailableJobs(string questGroupFilter)
    {
        currentQuestStateMask = AvailableQuestStateMask;
        _availableJobsGroupFilter = NormalizeQuestGroup(questGroupFilter);

        if (isOpen)
        {
            ShowQuests(AvailableQuestStateMask);
            return;
        }

        Open();
    }

    public override void ClickShowActiveQuests(object data)
    {
        _availableJobsGroupFilter = string.Empty;
        base.ClickShowActiveQuests(data);
    }

    public override void ClickShowCompletedQuests(object data)
    {
        _availableJobsGroupFilter = string.Empty;
        base.ClickShowCompletedQuests(data);
    }

    public override bool IsQuestVisible(string questTitle)
    {
        if (!base.IsQuestVisible(questTitle))
            return false;

        if (!IsShowingAvailableJobs || string.IsNullOrWhiteSpace(_availableJobsGroupFilter))
            return true;

        return string.Equals(
            NormalizeQuestGroup(QuestLog.GetQuestGroup(questTitle)),
            _availableJobsGroupFilter,
            StringComparison.Ordinal);
    }

    protected override string GetNoQuestsMessage(QuestState questStateMask)
    {
        return questStateMask == AvailableQuestStateMask
            ? GetLocalizedText(_noAvailableJobsText)
            : base.GetNoQuestsMessage(questStateMask);
    }

    protected override StandardUIQuestTitleButtonTemplate GetQuestTitleTemplate(QuestInfo quest)
    {
        if (IsShowingAvailableJobs && _useActiveQuestTemplatesForAvailableJobs)
            return activeQuestHeadingTemplate;

        return base.GetQuestTitleTemplate(quest);
    }

    protected override StandardUIQuestTitleButtonTemplate GetSelectedQuestTitleTemplate(QuestInfo quest)
    {
        if (IsShowingAvailableJobs && _useActiveQuestTemplatesForAvailableJobs)
            return selectedActiveQuestHeadingTemplate ?? activeQuestHeadingTemplate;

        return base.GetSelectedQuestTitleTemplate(quest);
    }

    protected override void RepaintSelectedQuest(QuestInfo quest)
    {
        base.RepaintSelectedQuest(quest);

        if (!IsShowingAvailableJobs || quest == null || abandonButtonTemplate == null)
            return;

        // Basic Pixel Crushers prefabs only provide one action-button template, so reuse it for "Accept Job" until the UI is polished.
        StandardUIButtonTemplate acceptButtonInstance = detailsPanelContentManager.Instantiate<StandardUIButtonTemplate>(abandonButtonTemplate);
        bool canAcceptFromSource = CanAcceptQuestFromSource(quest.Title, out _);
        acceptButtonInstance.Assign(canAcceptFromSource ? _acceptAvailableJobButtonText : _sourceBlockedJobButtonText);
        detailsPanelContentManager.Add(acceptButtonInstance, questDetailsContentContainer);
        acceptButtonInstance.button.interactable = canAcceptFromSource;

        if (canAcceptFromSource)
            acceptButtonInstance.button.onClick.AddListener(ClickAcceptAvailableJobButton);
    }

    public virtual void ClickAcceptAvailableJobButton()
    {
        AcceptAvailableJob(selectedQuest);
    }

    private void UpdateAvailableJobsViewState()
    {
        BindAvailableJobsButton();

        if (_showingAvailableJobsHeading != null)
            _showingAvailableJobsHeading.SetActive(IsShowingAvailableJobs);

        if (IsShowingAvailableJobs)
        {
            if (showingActiveQuestsHeading != null)
                showingActiveQuestsHeading.SetActive(false);

            if (showingCompletedQuestHeading != null)
                showingCompletedQuestHeading.SetActive(false);
        }

        if (_availableJobsButton != null)
            _availableJobsButton.interactable = !IsShowingAvailableJobs;

        if (IsShowingAvailableJobs)
        {
            if (activeQuestsButton != null)
                activeQuestsButton.interactable = true;

            if (completedQuestsButton != null)
                completedQuestsButton.interactable = true;
        }
    }

    private void EnsureAvailableJobsButton()
    {
        if (_availableJobsButton != null || !_createAvailableJobsButtonIfMissing || activeQuestsButton == null)
            return;

        Button availableButton = Instantiate(activeQuestsButton, activeQuestsButton.transform.parent, false);
        availableButton.name = "Available Jobs Button";
        availableButton.transform.SetSiblingIndex(activeQuestsButton.transform.GetSiblingIndex());
        availableButton.onClick = new Button.ButtonClickedEvent();
        availableButton.onClick.AddListener(ClickShowAvailableJobsButton);
        SetButtonText(availableButton, _availableJobsButtonText);

        _availableJobsButton = availableButton;
        _availableJobsButtonBound = true;
    }

    private void BindAvailableJobsButton()
    {
        if (_availableJobsButton == null || _availableJobsButtonBound)
            return;

        _availableJobsButton.onClick.AddListener(ClickShowAvailableJobsButton);
        _availableJobsButtonBound = true;
    }

    private void AcceptAvailableJob(string questTitle)
    {
        if (string.IsNullOrWhiteSpace(questTitle) || QuestLog.GetQuestState(questTitle) != QuestState.Grantable)
            return;

        if (!CanAcceptQuestFromSource(questTitle, out string activeQuestFromSameSource))
        {
#if UNITY_EDITOR
            Debug.Log($"[PixelCrushersQuestJournalWindow] Blocked available job '{questTitle}' because '{activeQuestFromSameSource}' is already active from the same quest group.", this);
#endif
            return;
        }

        QuestLog.SetQuestState(questTitle, QuestState.Active);

        if (_acceptedQuestEntryNumber > 0 && QuestLog.GetQuestEntryCount(questTitle) >= _acceptedQuestEntryNumber)
            QuestLog.SetQuestEntryState(questTitle, _acceptedQuestEntryNumber, QuestState.Active);

#if UNITY_EDITOR
        Debug.Log($"[PixelCrushersQuestJournalWindow] Accepted available job '{questTitle}'.", this);
#endif

        ShowQuests(ActiveQuestStateMask);
        SelectQuest(questTitle);
    }

    private bool CanAcceptQuestFromSource(string questTitle, out string activeQuestFromSameSource)
    {
        activeQuestFromSameSource = string.Empty;

        if (!_enforceOneActiveQuestPerGroup)
            return true;

        string questGroup = NormalizeQuestGroup(QuestLog.GetQuestGroup(questTitle));
        if (string.IsNullOrWhiteSpace(questGroup))
            return true;

        string[] activeQuests = QuestLog.GetAllQuests(QuestState.Active, true, null);
        for (int i = 0; i < activeQuests.Length; i++)
        {
            string activeQuest = activeQuests[i];
            if (string.Equals(activeQuest, questTitle))
                continue;

            if (string.Equals(NormalizeQuestGroup(QuestLog.GetQuestGroup(activeQuest)), questGroup))
            {
                activeQuestFromSameSource = activeQuest;
                return false;
            }
        }

        return true;
    }

    private static string NormalizeQuestGroup(string questGroup)
    {
        if (string.IsNullOrWhiteSpace(questGroup) || string.Equals(questGroup, "nil"))
            return string.Empty;

        return questGroup.Trim();
    }

    private static void SetButtonText(Button button, string text)
    {
        if (button == null)
            return;

        Text[] labels = button.GetComponentsInChildren<Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].text = text;
        }
    }

    private void RequestGameplayInputLock()
    {
        if (_uiEvents == null || _gameplayInputLocked || string.IsNullOrWhiteSpace(_gameplayInputLockId))
            return;

        _uiEvents.OnGameplayInputLockRequested?.Invoke(_gameplayInputLockId);
        _gameplayInputLocked = true;
    }

    private void ReleaseGameplayInputLock()
    {
        if (_uiEvents == null || !_gameplayInputLocked || string.IsNullOrWhiteSpace(_gameplayInputLockId))
            return;

        _uiEvents.OnGameplayInputUnlockRequested?.Invoke(_gameplayInputLockId);
        _gameplayInputLocked = false;
    }
}
