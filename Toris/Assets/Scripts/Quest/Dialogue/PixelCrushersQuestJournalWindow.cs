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

    private bool _availableJobsButtonBound;

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

    public virtual void ClickShowAvailableJobs(object data)
    {
        ShowQuests(AvailableQuestStateMask);
    }

    public virtual void ClickShowAvailableJobsButton()
    {
        ClickShowAvailableJobs(null);
    }

    public virtual void OpenAvailableJobs()
    {
        currentQuestStateMask = AvailableQuestStateMask;

        if (isOpen)
        {
            ShowQuests(AvailableQuestStateMask);
            return;
        }

        Open();
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
        acceptButtonInstance.Assign(_acceptAvailableJobButtonText);
        detailsPanelContentManager.Add(acceptButtonInstance, questDetailsContentContainer);
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

        QuestLog.SetQuestState(questTitle, QuestState.Active);

        if (_acceptedQuestEntryNumber > 0 && QuestLog.GetQuestEntryCount(questTitle) >= _acceptedQuestEntryNumber)
            QuestLog.SetQuestEntryState(questTitle, _acceptedQuestEntryNumber, QuestState.Active);

#if UNITY_EDITOR
        Debug.Log($"[PixelCrushersQuestJournalWindow] Accepted available job '{questTitle}'.", this);
#endif

        ShowQuests(ActiveQuestStateMask);
        SelectQuest(questTitle);
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
}
