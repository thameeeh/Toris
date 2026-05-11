using UnityEngine;

public abstract class PlayerVfxModule : ScriptableObject
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public virtual void Initialize(in PlayerVfxContext ctx) { }
    public virtual void Dispose(in PlayerVfxContext ctx) { }
    public virtual void Tick(in PlayerVfxContext ctx, float unscaledDeltaTime) { }

    public virtual void OnBowDrawStarted(in PlayerVfxContext ctx) { }
    public virtual void OnBowShootReady(in PlayerVfxContext ctx) { }
    public virtual void OnBowShotReleased(in PlayerVfxContext ctx) { }
    public virtual void OnBowShotFired(in PlayerVfxContext ctx) { }
    public virtual void OnBowDryReleased(in PlayerVfxContext ctx) { }

    public virtual void OnDashStarted(in PlayerVfxContext ctx, Vector2 direction) { }
    public virtual void OnDashCompleted(in PlayerVfxContext ctx) { }

    protected static Quaternion RotationFromDirection2D(Vector2 direction, Quaternion fallback)
    {
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            return fallback;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        return Quaternion.Euler(0f, 0f, angle);
    }

    protected static Quaternion ToLocalRotation(Transform parent, Quaternion worldRotation)
    {
        return parent != null
            ? Quaternion.Inverse(parent.rotation) * worldRotation
            : worldRotation;
    }
}

public readonly struct PlayerVfxContext
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    public readonly PlayerVfx Hub;
    public readonly Transform Transform;
    public readonly PlayerBowController Bow;
    public readonly DashAbility Dash;
    public readonly PlayerMotor Motor;
    public readonly Rigidbody2D Rb;
    public readonly PlayerFacing Facing;

    public PlayerVfxContext(
        PlayerVfx hub,
        Transform transform,
        PlayerBowController bow,
        DashAbility dash,
        PlayerMotor motor,
        Rigidbody2D rb,
        PlayerFacing facing)
    {
        Hub = hub;
        Transform = transform;
        Bow = bow;
        Dash = dash;
        Motor = motor;
        Rb = rb;
        Facing = facing;
    }

    public bool HasEffects =>
        EffectManagerBehavior.Instance != null &&
        EffectManagerBehavior.Instance != NullEffectManager.Instance;

    public Vector2 CurrentFacingDirection
    {
        get
        {
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
