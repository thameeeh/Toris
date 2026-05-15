using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerVfx))]
public sealed class PlayerVfxEventBridge : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerVfx playerVfx;
    [SerializeField] private PlayerBowController bow;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerFacing facing;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerStatusController statusController;

    private DashAbility boundDash;
    private bool eventsBound;
    private bool hasHealthSnapshot;
    private bool hasStaminaSnapshot;
    private float previousHealth;
    private float previousStamina;

    private void Awake()
    {
        ResolveDependencies();
        InitializeResourceSnapshots();
    }

    private void Reset()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        InitializeResourceSnapshots();
        BindEvents();
        playerVfx?.InitializeRuntime(CreateContext(PlayerVfxEventType.None));
    }

    private void OnDisable()
    {
        playerVfx?.DisposeRuntime(CreateContext(PlayerVfxEventType.None));
        UnbindEvents();
    }

    private void OnValidate()
    {
        ResolveDependencies();
    }

    private void ResolveDependencies()
    {
        if (playerVfx == null)
            TryGetComponent(out playerVfx);

        if (bow == null)
            TryGetComponent(out bow);

        if (playerController == null)
            TryGetComponent(out playerController);

        if (motor == null)
            TryGetComponent(out motor);

        if (rb == null)
            TryGetComponent(out rb);

        if (facing == null)
            TryGetComponent(out facing);

        if (stats == null)
            TryGetComponent(out stats);

        if (statusController == null)
            TryGetComponent(out statusController);
    }

    private void InitializeResourceSnapshots()
    {
        if (stats == null)
            return;

        previousHealth = stats.currentHP;
        previousStamina = stats.currentStamina;
        hasHealthSnapshot = true;
        hasStaminaSnapshot = true;
    }

    private void BindEvents()
    {
        if (eventsBound)
            return;

        if (bow != null)
        {
            bow.DrawStarted += HandleBowDrawStarted;
            bow.ShootReady += HandleBowShootReady;
            bow.ShotReleased += HandleBowShotReleased;
            bow.ShotFired += HandleBowShotFired;
            bow.DryReleased += HandleBowDryReleased;
        }

        if (motor != null)
        {
            motor.DashStarted += HandleDashStarted;
        }

        boundDash = playerController != null
            ? playerController.DashAbility
            : motor != null ? motor.DashAbility : null;

        if (boundDash != null)
        {
            boundDash.Completed += HandleDashCompleted;
        }

        if (stats != null)
        {
            stats.OnHealthChanged += HandleHealthChanged;
            stats.OnStaminaChanged += HandleStaminaChanged;
            stats.OnPlayerDied += HandlePlayerDied;
        }

        if (statusController != null)
        {
            statusController.OnStatusApplied += HandleStatusApplied;
            statusController.OnStatusRemoved += HandleStatusRemoved;
            statusController.OnStatusDamageTick += HandleStatusDamageTick;
        }

        eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!eventsBound)
            return;

        if (bow != null)
        {
            bow.DrawStarted -= HandleBowDrawStarted;
            bow.ShootReady -= HandleBowShootReady;
            bow.ShotReleased -= HandleBowShotReleased;
            bow.ShotFired -= HandleBowShotFired;
            bow.DryReleased -= HandleBowDryReleased;
        }

        if (motor != null)
        {
            motor.DashStarted -= HandleDashStarted;
        }

        if (boundDash != null)
        {
            boundDash.Completed -= HandleDashCompleted;
            boundDash = null;
        }

        if (stats != null)
        {
            stats.OnHealthChanged -= HandleHealthChanged;
            stats.OnStaminaChanged -= HandleStaminaChanged;
            stats.OnPlayerDied -= HandlePlayerDied;
        }

        if (statusController != null)
        {
            statusController.OnStatusApplied -= HandleStatusApplied;
            statusController.OnStatusRemoved -= HandleStatusRemoved;
            statusController.OnStatusDamageTick -= HandleStatusDamageTick;
        }

        eventsBound = false;
    }

    private void HandleBowDrawStarted()
    {
        Emit(PlayerVfxEventType.BowDrawStarted);
    }

    private void HandleBowShootReady()
    {
        Emit(PlayerVfxEventType.BowShootReady);
    }

    private void HandleBowShotReleased()
    {
        Emit(PlayerVfxEventType.BowShotReleased);
    }

    private void HandleBowShotFired()
    {
        Emit(PlayerVfxEventType.BowShotFired);
    }

    private void HandleBowDryReleased()
    {
        Emit(PlayerVfxEventType.BowDryReleased);
    }

    private void HandleDashStarted(Vector2 direction)
    {
        Emit(CreateContext(
            PlayerVfxEventType.DashStarted,
            PlayerVfxResourceKind.None,
            transform.position,
            direction,
            0f,
            0f,
            0f,
            default,
            false));
    }

    private void HandleDashCompleted()
    {
        Emit(PlayerVfxEventType.DashCompleted);
    }

    private void HandleHealthChanged(float current, float maximum)
    {
        float delta = hasHealthSnapshot ? current - previousHealth : 0f;
        previousHealth = current;
        hasHealthSnapshot = true;

        EmitResource(PlayerVfxEventType.HealthChanged, PlayerVfxResourceKind.Health, delta, current, maximum);

        if (delta > 0f)
        {
            EmitResource(PlayerVfxEventType.Healed, PlayerVfxResourceKind.Health, delta, current, maximum);
        }
        else if (delta < 0f)
        {
            EmitResource(PlayerVfxEventType.Damaged, PlayerVfxResourceKind.Health, -delta, current, maximum);
        }
    }

    private void HandleStaminaChanged(float current, float maximum)
    {
        float delta = hasStaminaSnapshot ? current - previousStamina : 0f;
        previousStamina = current;
        hasStaminaSnapshot = true;

        EmitResource(PlayerVfxEventType.StaminaChanged, PlayerVfxResourceKind.Stamina, delta, current, maximum);

        if (delta > 0f)
        {
            EmitResource(PlayerVfxEventType.StaminaRestored, PlayerVfxResourceKind.Stamina, delta, current, maximum);
        }
        else if (delta < 0f)
        {
            EmitResource(PlayerVfxEventType.StaminaSpent, PlayerVfxResourceKind.Stamina, -delta, current, maximum);
        }
    }

    private void HandlePlayerDied()
    {
        Emit(PlayerVfxEventType.PlayerDied);
    }

    private void HandleStatusApplied(PlayerStatusEffectType statusType)
    {
        EmitStatus(PlayerVfxEventType.StatusApplied, statusType, 0f);
    }

    private void HandleStatusRemoved(PlayerStatusEffectType statusType)
    {
        EmitStatus(PlayerVfxEventType.StatusRemoved, statusType, 0f);
    }

    private void HandleStatusDamageTick(PlayerStatusEffectType statusType, float damage)
    {
        EmitStatus(PlayerVfxEventType.StatusDamageTick, statusType, damage);
    }

    private void Emit(PlayerVfxEventType eventType)
    {
        Emit(CreateContext(eventType));
    }

    private void EmitResource(
        PlayerVfxEventType eventType,
        PlayerVfxResourceKind resourceKind,
        float amount,
        float current,
        float maximum)
    {
        Emit(CreateContext(
            eventType,
            resourceKind,
            transform.position,
            Vector2.zero,
            amount,
            current,
            maximum,
            default,
            false));
    }

    private void EmitStatus(PlayerVfxEventType eventType, PlayerStatusEffectType statusType, float amount)
    {
        Emit(CreateContext(
            eventType,
            PlayerVfxResourceKind.None,
            transform.position,
            Vector2.zero,
            amount,
            0f,
            0f,
            statusType,
            true));
    }

    private void Emit(in PlayerVfxEventContext context)
    {
        playerVfx?.HandleEvent(context);
    }

    private PlayerVfxEventContext CreateContext(PlayerVfxEventType eventType)
    {
        return CreateContext(
            eventType,
            PlayerVfxResourceKind.None,
            transform.position,
            Vector2.zero,
            0f,
            0f,
            0f,
            default,
            false);
    }

    private PlayerVfxEventContext CreateContext(
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
        DashAbility dash = playerController != null
            ? playerController.DashAbility
            : motor != null ? motor.DashAbility : null;

        return new PlayerVfxEventContext(
            playerVfx,
            transform,
            bow,
            playerController,
            dash,
            motor,
            rb,
            facing,
            stats,
            statusController,
            eventType,
            resourceKind,
            worldPosition,
            direction,
            amount,
            currentValue,
            maxValue,
            statusType,
            hasStatusType);
    }
}
