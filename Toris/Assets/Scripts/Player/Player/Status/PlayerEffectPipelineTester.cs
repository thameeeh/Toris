using UnityEngine;

// context menu tester; purely testing
public class PlayerEffectPipelineTester : MonoBehaviour
{
    [SerializeField] private PlayerEffectSourceController _effectSourceController;
    [SerializeField] private PlayerStats _playerStats;

    [Header("Test Effects")]
    [SerializeField] private PlayerEffectDefinitionSO _blueGemEffect;
    [SerializeField] private PlayerEffectDefinitionSO _redGemEffect;
    [SerializeField] private PlayerEffectDefinitionSO _greenGemEffect;
    [SerializeField] private PlayerEffectDefinitionSO _purpleGemEffect;

    [ContextMenu("Apply Blue Gem To Slot 1")]
    public void ApplyBlueGem()
    {
        _effectSourceController.SetSource("GemSlot1", _blueGemEffect);
        PrintState("Applied Blue Gem");
    }

    [ContextMenu("Apply Red Gem To Slot 2")]
    public void ApplyRedGem()
    {
        _effectSourceController.SetSource("GemSlot2", _redGemEffect);
        PrintState("Applied Red Gem");
    }

    [ContextMenu("Apply Green Gem To Slot 3")]
    public void ApplyGreenGem()
    {
        _effectSourceController.SetSource("GemSlot3", _greenGemEffect);
        PrintState("Applied Green Gem");
    }

    [ContextMenu("Apply Purple Gem To Slot 4")]
    public void ApplyPurpleGem()
    {
        _effectSourceController.SetSource("GemSlot4", _purpleGemEffect);
        PrintState("Applied Purple Gem");
    }

    [ContextMenu("Remove Slot 1")]
    public void RemoveSlot1()
    {
        _effectSourceController.RemoveSource("GemSlot1");
        PrintState("Removed Slot 1");
    }

    [ContextMenu("Clear All Sources")]
    public void ClearAllSources()
    {
        _effectSourceController.ClearAllSources();
        PrintState("Cleared All Sources");
    }

    [ContextMenu("Print Current State")]
    public void PrintCurrentState()
    {
        PrintState("Manual Print");
    }

    private void PrintState(string label)
    {
        if (_effectSourceController == null || _playerStats == null)
        {
            Debug.LogWarning("[PlayerEffectPipelineTester] Missing references.", this);
            return;
        }

        PlayerResolvedEffects effects = _effectSourceController.ResolvedEffects;

        Debug.Log(
            $"[{label}]\n" +
            $"HP: {_playerStats.currentHP}/{_playerStats.maxHP}\n" +
            $"Stamina: {_playerStats.currentStamina}/{_playerStats.maxStamina}\n" +
            $"Stamina Regen: {_playerStats.staminaRegenPerSec}\n" +
            $"Move Speed Mult: {effects.moveSpeedMultiplier}\n" +
            $"Dash Speed Mult: {effects.dashSpeedMultiplier}\n" +
            $"Outgoing Damage Mult: {effects.outgoingDamageMultiplier}\n" +
            $"Incoming Damage Mult: {effects.incomingDamageMultiplier}\n" +
            $"Poison Immune: {effects.isPoisonImmune}\n" +
            $"Burning Immune: {effects.isBurningImmune}\n" +
            $"Bleeding Immune: {effects.isBleedingImmune}",
            this);
    }
}
