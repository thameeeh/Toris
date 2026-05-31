using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Overhead quest indicator controller for Outland Haven NPCs.
/// Inherits from Dialogue System's QuestStateListener to automatically hook into quest updates.
/// </summary>
public class QuestIndicatorController : QuestStateListener
{
    [Header("Visual Indicators")]
    [Tooltip("Active when quest is Unassigned (Available to take). Usually an exclamation mark !")]
    [SerializeField] private GameObject _availableIndicator;
    
    [Tooltip("Active when quest is Active (In Progress). Usually a grayed out ? or left null to hide.")]
    [SerializeField] private GameObject _inProgressIndicator;
    
    [Tooltip("Active when quest is ReturnToNPC (Ready to turn in). Usually a glowing question mark ?")]
    [SerializeField] private GameObject _readyToTurnInIndicator;

    public override void UpdateIndicator()
    {
        // First invoke any standard inspector events or indicator levels configured on the base listener
        base.UpdateIndicator();
        
        UpdateIndicatorState();
    }
    
    public void UpdateIndicatorState()
    {
        if (string.IsNullOrWhiteSpace(questName))
        {
            SetAllIndicatorsActive(false);
            return;
        }
        
        QuestState state = QuestLog.GetQuestState(questName);
        
        bool isAvailable = state == QuestState.Unassigned;
        bool isInProgress = state == QuestState.Active;
        bool isReadyToTurnIn = state == QuestState.ReturnToNPC;
        
        if (_availableIndicator != null) _availableIndicator.SetActive(isAvailable);
        if (_inProgressIndicator != null) _inProgressIndicator.SetActive(isInProgress);
        if (_readyToTurnInIndicator != null) _readyToTurnInIndicator.SetActive(isReadyToTurnIn);
    }
    
    private void SetAllIndicatorsActive(bool active)
    {
        if (_availableIndicator != null) _availableIndicator.SetActive(active);
        if (_inProgressIndicator != null) _inProgressIndicator.SetActive(active);
        if (_readyToTurnInIndicator != null) _readyToTurnInIndicator.SetActive(active);
    }
}
