using System;

[Serializable]
public class PlayerStatusInstance
{
    public PlayerStatusEffectType StatusType { get; private set; }
    public float DamagePerSecond { get; private set; }
    public float RemainingDuration { get; private set; }
    public float TickInterval { get; private set; }
    public float TickTimer { get; private set; }
    public int Stacks { get; private set; }

    public bool IsExpired => RemainingDuration <= 0f;

    public void Initialize(
        PlayerStatusEffectType statusType,
        float damagePerSecond,
        float duration,
        float tickInterval,
        int stacks)
    {
        StatusType = statusType;
        DamagePerSecond = Math.Max(0f, damagePerSecond);
        RemainingDuration = Math.Max(0f, duration);
        TickInterval = Math.Max(0.01f, tickInterval);
        TickTimer = TickInterval;
        Stacks = Math.Max(1, stacks);
    }

    public void Refresh(
        float damagePerSecond,
        float duration,
        float tickInterval,
        int stacks,
        bool preserveHigherDamage = true,
        bool preserveHigherStacks = true)
    {
        float validatedDamage = Math.Max(0f, damagePerSecond);
        float validatedDuration = Math.Max(0f, duration);
        float validatedTickInterval = Math.Max(0.01f, tickInterval);
        int validatedStacks = Math.Max(1, stacks);

        DamagePerSecond = preserveHigherDamage ? Math.Max(DamagePerSecond, validatedDamage) : validatedDamage;
        RemainingDuration = Math.Max(RemainingDuration, validatedDuration);
        TickInterval = validatedTickInterval;
        TickTimer = Math.Min(TickTimer, TickInterval);
        Stacks = preserveHigherStacks ? Math.Max(Stacks, validatedStacks) : validatedStacks;
    }

    public bool Tick(float deltaTime, out float damageToApply)
    {
        damageToApply = 0f;

        if (IsExpired)
            return false;

        RemainingDuration = Math.Max(0f, RemainingDuration - deltaTime);
        TickTimer -= deltaTime;

        bool triggered = false;

        while (TickTimer <= 0f && !IsExpired)
        {
            float damagePerTick = DamagePerSecond * TickInterval * Stacks;
            damageToApply += damagePerTick;
            TickTimer += TickInterval;
            triggered = true;
        }

        return triggered;
    }
}