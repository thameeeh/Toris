using UnityEngine;

public struct PlayerAbilityContext
{
    public PlayerAbilityController controller;
    public PlayerStats stats;
    public PlayerBowController bow;
    public PlayerInputReaderSO input;
}

public abstract class PlayerAbilitySO : ScriptableObject
{
    [Header("UI / Metada")]
    public string abilityName = "New Ability";
    public Sprite icon;

    [Header("Cooldown")]
    [Min(0f)] public float cooldownSeconds = 0f;

    protected float _nextReadyTime = 0f;
    protected bool isOnCooldown => Time.time < _nextReadyTime;

    protected void StartCooldown()
    {
        _nextReadyTime = Time.time + cooldownSeconds;
    }

    /// <summary>
    /// Reset runtime cooldown state. Call this at game start so abilities
    /// don't stay stuck on cooldown from a previous play session.
    /// </summary>
    public void ResetCooldown()
    {
        _nextReadyTime = 0f;
    }

    /// <summary>
    /// Override this if the ability should be locked behind some progress.
    /// Default: always unlocked.
    /// </summary>
    public virtual bool IsUnlocked(PlayerAbilityContext context) => true;
    public virtual void OnButtonDown(PlayerAbilityContext context) { }
    public virtual void OnButtonUp(PlayerAbilityContext context) { }
    public virtual void Tick(PlayerAbilityContext context) { }

    public virtual bool IsReady(PlayerAbilityContext context)
    {
        return IsUnlocked(context) && !isOnCooldown;
    }
}
