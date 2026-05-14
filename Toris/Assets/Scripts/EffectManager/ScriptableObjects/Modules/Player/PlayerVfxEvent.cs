using UnityEngine;

public enum PlayerVfxEventType
{
    None = 0,
    BowDrawStarted = 10,
    BowShootReady = 11,
    BowShotReleased = 12,
    BowShotFired = 13,
    BowDryReleased = 14,
    BowImpact = 15,
    DashStarted = 20,
    DashCompleted = 21,
    HealthChanged = 30,
    Healed = 31,
    Damaged = 32,
    StaminaChanged = 40,
    StaminaRestored = 41,
    StaminaSpent = 42,
    PlayerDied = 50,
    StatusApplied = 60,
    StatusRemoved = 61,
    StatusDamageTick = 62
}

public enum PlayerVfxResourceKind
{
    None,
    Health,
    Stamina
}

public readonly struct PlayerVfxEventContext
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public readonly PlayerVfx Hub;
    public readonly Transform Transform;
    public readonly PlayerBowController Bow;
    public readonly PlayerController PlayerController;
    public readonly DashAbility Dash;
    public readonly PlayerMotor Motor;
    public readonly Rigidbody2D Rb;
    public readonly PlayerFacing Facing;
    public readonly PlayerStats Stats;
    public readonly PlayerStatusController StatusController;
    public readonly PlayerVfxEventType EventType;
    public readonly PlayerVfxResourceKind ResourceKind;
    public readonly Vector3 WorldPosition;
    public readonly Vector2 Direction;
    public readonly float Amount;
    public readonly float CurrentValue;
    public readonly float MaxValue;
    public readonly PlayerStatusEffectType StatusType;
    public readonly bool HasStatusType;

    public PlayerVfxEventContext(
        PlayerVfx hub,
        Transform transform,
        PlayerBowController bow,
        PlayerController playerController,
        DashAbility dash,
        PlayerMotor motor,
        Rigidbody2D rb,
        PlayerFacing facing,
        PlayerStats stats,
        PlayerStatusController statusController,
        PlayerVfxEventType eventType,
        PlayerVfxResourceKind resourceKind,
        Vector3 worldPosition,
        Vector2 direction,
        float amount,
        float currentValue,
        float maxValue,
        PlayerStatusEffectType statusType,
        bool hasStatusType)
    {
        Hub = hub;
        Transform = transform;
        Bow = bow;
        PlayerController = playerController;
        Dash = dash;
        Motor = motor;
        Rb = rb;
        Facing = facing;
        Stats = stats;
        StatusController = statusController;
        EventType = eventType;
        ResourceKind = resourceKind;
        WorldPosition = worldPosition;
        Direction = direction.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE ? direction.normalized : Vector2.zero;
        Amount = amount;
        CurrentValue = currentValue;
        MaxValue = maxValue;
        StatusType = statusType;
        HasStatusType = hasStatusType;
    }

    public bool HasEffects =>
        EffectManagerBehavior.Instance != null &&
        EffectManagerBehavior.Instance != NullEffectManager.Instance;

    public Vector2 CurrentFacingDirection
    {
        get
        {
            if (Direction.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                return Direction;

            if (Bow != null)
            {
                Vector2 aimDirection = Bow.CurrentAimDirection;
                if (aimDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                    return aimDirection.normalized;
            }

            if (Facing != null && Facing.CurrentFacing.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                return Facing.CurrentFacing;

            if (Motor != null && Motor.CurrentMoveInput.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                return Motor.CurrentMoveInput.normalized;

            if (Rb != null)
            {
#if UNITY_2022_1_OR_NEWER
                Vector2 velocity = Rb.linearVelocity;
#else
                Vector2 velocity = Rb.velocity;
#endif
                if (velocity.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
                    return velocity.normalized;
            }

            return Vector2.down;
        }
    }
}
