using UnityEngine;

public class PlayerResolvedEffectsDebugger : MonoBehaviour
{
    [SerializeField] private PlayerStats _playerStats;

    [ContextMenu("Log Resolved Effects")]
    public void LogResolvedEffects()
    {
        if (_playerStats == null)
        {
            Debug.LogWarning("[PlayerResolvedEffectsDebugger] Missing PlayerStats reference.");
            return;
        }

        PlayerResolvedEffects effects = _playerStats.ResolvedEffects;

        Debug.Log(
            $"[PlayerResolvedEffectsDebugger]\n" +
            $"MaxHealth: {effects.maxHealth}\n" +
            $"MaxStamina: {effects.maxStamina}\n" +
            $"StaminaRegen: {effects.staminaRegenPerSecond}\n" +
            $"MoveSpeedMultiplier: {effects.moveSpeedMultiplier}\n" +
            $"DashSpeedMultiplier: {effects.dashSpeedMultiplier}\n" +
            $"OutgoingDamageMultiplier: {effects.outgoingDamageMultiplier}\n" +
            $"IncomingDamageMultiplier: {effects.incomingDamageMultiplier}"
        );
    }
}
