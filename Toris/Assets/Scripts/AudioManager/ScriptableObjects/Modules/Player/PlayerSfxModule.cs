using UnityEngine;

public abstract class PlayerSfxModule : ScriptableObject
{
    public virtual void Initialize(in PlayerSfxContext ctx) { }
    public virtual void Tick(in PlayerSfxContext ctx, float unscaledDeltaTime) { }

    // Events the hub can forward (add more as you need)
    public virtual void OnBowDrawStarted(in PlayerSfxContext ctx) { }
    public virtual void OnBowShotFired(in PlayerSfxContext ctx) { }
    public virtual void OnBowDryReleased(in PlayerSfxContext ctx) { }

    public virtual void OnDashStarted(in PlayerSfxContext ctx) { }
    public virtual void OnDashCompleted(in PlayerSfxContext ctx) { }
}

public readonly struct PlayerSfxContext
{
    public readonly PlayerSfx Hub;
    public readonly Transform Transform;

    public readonly PlayerBowController Bow;
    public readonly DashAbility Dash;
    public readonly PlayerMotor Motor;
    public readonly Rigidbody2D Rb;

    public PlayerSfxContext(
        PlayerSfx hub,
        Transform transform,
        PlayerBowController bow,
        DashAbility dash,
        PlayerMotor motor,
        Rigidbody2D rb)
    {
        Hub = hub;
        Transform = transform;
        Bow = bow;
        Dash = dash;
        Motor = motor;
        Rb = rb;
    }

    public bool HasAudio => AudioBootstrap.Sfx != null;
}
