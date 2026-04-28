using System;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;

/// <summary>
/// Opens the Pixel Crushers quest journal from a Toris interaction source.
/// Use this for job boards or world objects that should expose available quests without dialogue.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersQuestJournalInteractable : MonoBehaviour, IInteractable
{
    [Tooltip("Project UI event channel used to request the Pixel Crushers quest journal.")]
    [SerializeField] private UIEventsSO _uiEvents;
    [Tooltip("Journal mode to open. Use Available:All for a board that can accept every currently grantable job.")]
    [SerializeField] private string _journalMode = "Available:All";
    [Tooltip("Optional quests to mark Grantable before opening the journal. Leave empty if quests are unlocked elsewhere.")]
    [SerializeField] private string[] _questsToMarkGrantable = Array.Empty<string>();
    [Tooltip("Only mark configured quests Grantable when they are currently Unassigned.")]
    [SerializeField] private bool _onlyMarkUnassignedQuests = true;

#if UNITY_EDITOR
    [Tooltip("Logs job board journal interactions. Editor only.")]
    [SerializeField] private bool _debugInteraction = true;
#endif

    public void Interact(GameObject interactor)
    {
        MarkConfiguredQuestsGrantable();

        if (_uiEvents == null)
        {
            LogWarning("Cannot open quest journal because UIEventsSO is not assigned.");
            return;
        }

        _uiEvents.OnQuestJournalOpenRequested?.Invoke(_journalMode);
        LogDebug($"Requested quest journal mode '{_journalMode}'.");
    }

    private void MarkConfiguredQuestsGrantable()
    {
        if (_questsToMarkGrantable == null)
            return;

        for (int i = 0; i < _questsToMarkGrantable.Length; i++)
            TryMarkQuestGrantable(_questsToMarkGrantable[i]);
    }

    private void TryMarkQuestGrantable(string questName)
    {
        if (string.IsNullOrWhiteSpace(questName))
            return;

        QuestState currentState = PixelCrushersQuestBridge.GetQuestState(questName);
        if (_onlyMarkUnassignedQuests && currentState != QuestState.Unassigned)
            return;

        if (currentState == QuestState.Active || currentState == QuestState.ReturnToNPC || currentState == QuestState.Success)
            return;

        PixelCrushersQuestBridge.SetQuestState(questName, QuestState.Grantable);
        LogDebug($"Marked quest '{questName}' Grantable.");
    }

    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        if (_debugInteraction)
            Debug.Log($"[PixelCrushersQuestJournalInteractable] {message}", this);
#endif
    }

    private void LogWarning(string message)
    {
#if UNITY_EDITOR
        if (_debugInteraction)
            Debug.LogWarning($"[PixelCrushersQuestJournalInteractable] {message}", this);
#endif
    }
}
