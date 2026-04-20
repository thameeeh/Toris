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
    [Header("UI / Metadata")]
    public string abilityName = "New Ability";
    public Sprite icon;

    [Header("Cooldown")]
    [Min(0f)] public float cooldownSeconds = 0f;

    public virtual PlayerAbilityRuntime CreateRuntime()
    {
        return new PlayerAbilityRuntime();
    }
    public virtual bool IsUnlocked(PlayerAbilityContext context) => true;

    public virtual void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context) { }
    public virtual void OnButtonUp(PlayerAbilityRuntime runtime, PlayerAbilityContext context) { }
    public virtual void Tick(PlayerAbilityRuntime runtime, PlayerAbilityContext context) { }
}