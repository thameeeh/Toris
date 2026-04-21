using UnityEngine;
using OutlandHaven.UIToolkit;

public struct PlayerAbilityContext
{
    public PlayerAbilityController controller;
    public PlayerStats stats;
    public PlayerBowController bow;
    public PlayerMotor motor;
    public PlayerInputReaderSO input;
    public GameSessionSO gameSession;
}

public abstract class PlayerAbilitySO : ScriptableObject
{
    [Header("Identity")]
    public string abilityID;
    public string requiredSkillID;

    [Header("UI / Metadata")]
    public string abilityName = "New Ability";
    public Sprite icon;

    [Header("Cooldown")]
    [Min(0f)] public float cooldownSeconds = 0f;

    [Header("Bow Draw Lock")]
    public bool blocksBowDraw = true;
    [Min(0f)] public float bowDrawLockDuration = 0.25f;

    [Header("Movement Lock")]
    public bool blocksMovement;
    [Min(0f)] public float movementLockDuration = 0.25f;

    public virtual PlayerAbilityRuntime CreateRuntime()
    {
        return new PlayerAbilityRuntime();
    }

    public virtual bool IsUnlocked(PlayerAbilityContext context)
    {
        if (string.IsNullOrWhiteSpace(requiredSkillID))
            return true;

        GameSessionSO gameSession = context.gameSession != null
            ? context.gameSession
            : GameSessionSO.LoadDefault();

        return gameSession != null
            && gameSession.PlayerSkills != null
            && gameSession.PlayerSkills.HasSkill(requiredSkillID);
    }

    public virtual void OnButtonDown(PlayerAbilityRuntime runtime, PlayerAbilityContext context) { }
    public virtual void OnButtonUp(PlayerAbilityRuntime runtime, PlayerAbilityContext context) { }
    public virtual void Tick(PlayerAbilityRuntime runtime, PlayerAbilityContext context) { }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(abilityID))
        {
            abilityID = name;
        }
    }
#endif
}
