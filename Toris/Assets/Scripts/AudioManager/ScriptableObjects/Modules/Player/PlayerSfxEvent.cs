using UnityEngine;

public enum PlayerSfxEventType
{
    None = 0,
    BowDrawStarted = 10,
    BowShootReady = 11,
    BowShotReleased = 12,
    BowShotFired = 13,
    BowDryReleased = 14,
    DashStarted = 20,
    DashCompleted = 21,
    MovementStarted = 30,
    MovementStopped = 31,
    HealthChanged = 40,
    Healed = 41,
    Damaged = 42,
    StaminaChanged = 50,
    StaminaRestored = 51,
    StaminaSpent = 52,
    PlayerDied = 60,
    StatusApplied = 70,
    StatusRemoved = 71,
    StatusDamageTick = 72,
    ConsumableUsed = 80,
    HealthConsumableUsed = 81,
    ManaConsumableUsed = 82,
    TimedConsumableUsed = 83
}

public enum PlayerSfxResourceKind
{
    None,
    Health,
    Stamina
}

public readonly struct PlayerSfxEventContext
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public readonly PlayerSfx Hub;
    public readonly Transform Transform;
    public readonly PlayerBowController Bow;
    public readonly PlayerController PlayerController;
    public readonly DashAbility Dash;
    public readonly PlayerMotor Motor;
    public readonly Rigidbody2D Rb;
    public readonly PlayerFacing Facing;
    public readonly PlayerStats Stats;
    public readonly PlayerStatusController StatusController;
    public readonly PlayerSfxEventType EventType;
    public readonly PlayerSfxResourceKind ResourceKind;
    public readonly Vector3 WorldPosition;
    public readonly Vector2 Direction;
    public readonly float Amount;
    public readonly float CurrentValue;
    public readonly float MaxValue;
    public readonly PlayerResourceChangeReason ResourceChangeReason;
    public readonly PlayerStatusEffectType StatusType;
    public readonly bool HasStatusType;

    public PlayerSfxEventContext(
        PlayerSfx hub,
        Transform transform,
        PlayerBowController bow,
        PlayerController playerController,
        DashAbility dash,
        PlayerMotor motor,
        Rigidbody2D rb,
        PlayerFacing facing,
        PlayerStats stats,
        PlayerStatusController statusController,
        PlayerSfxEventType eventType,
        PlayerSfxResourceKind resourceKind,
        Vector3 worldPosition,
        Vector2 direction,
        float amount,
        float currentValue,
        float maxValue,
        PlayerResourceChangeReason resourceChangeReason,
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
        ResourceChangeReason = resourceChangeReason;
        StatusType = statusType;
        HasStatusType = hasStatusType;
    }

    public bool HasAudio => AudioBootstrap.Sfx != null;

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
