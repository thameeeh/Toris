using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerSfx))]
public sealed class PlayerSfxEventBridge : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private PlayerSfx playerSfx;
    [SerializeField] private PlayerBowController bow;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerFacing facing;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private PlayerStatusController statusController;

    [Header("Movement Events")]
    [SerializeField] private float movementStartSpeed = 0.1f;

    private DashAbility boundDash;
    private bool eventsBound;
    private bool hasHealthSnapshot;
    private bool hasStaminaSnapshot;
    private bool wasMoving;
    private float previousHealth;
    private float previousStamina;

    private void Awake()
    {
        ResolveDependencies();
        InitializeResourceSnapshots();
        wasMoving = IsMovingForAudio();
    }

    private void Reset()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        InitializeResourceSnapshots();
        wasMoving = IsMovingForAudio();
        BindEvents();
        playerSfx?.InitializeRuntime(CreateContext(PlayerSfxEventType.None));
    }

    private void OnDisable()
    {
        playerSfx?.DisposeRuntime(CreateContext(PlayerSfxEventType.None));
        wasMoving = false;
        UnbindEvents();
    }

    private void Update()
    {
        bool isMoving = IsMovingForAudio();
        if (isMoving == wasMoving)
            return;

        wasMoving = isMoving;
        Emit(isMoving ? PlayerSfxEventType.MovementStarted : PlayerSfxEventType.MovementStopped);
    }

    private void OnValidate()
    {
        ResolveDependencies();
        movementStartSpeed = Mathf.Max(0f, movementStartSpeed);
    }

    private void ResolveDependencies()
    {
        if (playerSfx == null)
            TryGetComponent(out playerSfx);

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

    private bool IsMovingForAudio()
    {
        if (motor != null && motor.isDashing)
            return false;

        if (rb == null)
            return false;

#if UNITY_2022_1_OR_NEWER
        Vector2 velocity = rb.linearVelocity;
#else
        Vector2 velocity = rb.velocity;
#endif
        return velocity.sqrMagnitude > movementStartSpeed * movementStartSpeed;
    }

    private void HandleBowDrawStarted()
    {
        Emit(PlayerSfxEventType.BowDrawStarted);
    }

    private void HandleBowShootReady()
    {
        Emit(PlayerSfxEventType.BowShootReady);
    }

    private void HandleBowShotReleased()
    {
        Emit(PlayerSfxEventType.BowShotReleased);
    }

    private void HandleBowShotFired()
    {
        Emit(PlayerSfxEventType.BowShotFired);
    }

    private void HandleBowDryReleased()
    {
        Emit(PlayerSfxEventType.BowDryReleased);
    }

    private void HandleDashStarted(Vector2 direction)
    {
        if (wasMoving)
        {
            wasMoving = false;
            Emit(PlayerSfxEventType.MovementStopped);
        }

        Emit(CreateContext(
            PlayerSfxEventType.DashStarted,
            PlayerSfxResourceKind.None,
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
        Emit(PlayerSfxEventType.DashCompleted);
    }

    private void HandleHealthChanged(float current, float maximum)
    {
        float delta = hasHealthSnapshot ? current - previousHealth : 0f;
        previousHealth = current;
        hasHealthSnapshot = true;

        EmitResource(PlayerSfxEventType.HealthChanged, PlayerSfxResourceKind.Health, delta, current, maximum);

        if (delta > 0f)
        {
            EmitResource(PlayerSfxEventType.Healed, PlayerSfxResourceKind.Health, delta, current, maximum);
        }
        else if (delta < 0f)
        {
            EmitResource(PlayerSfxEventType.Damaged, PlayerSfxResourceKind.Health, -delta, current, maximum);
        }
    }

    private void HandleStaminaChanged(float current, float maximum)
    {
        float delta = hasStaminaSnapshot ? current - previousStamina : 0f;
        previousStamina = current;
        hasStaminaSnapshot = true;

        EmitResource(PlayerSfxEventType.StaminaChanged, PlayerSfxResourceKind.Stamina, delta, current, maximum);

        if (delta > 0f)
        {
            EmitResource(PlayerSfxEventType.StaminaRestored, PlayerSfxResourceKind.Stamina, delta, current, maximum);
        }
        else if (delta < 0f)
        {
            EmitResource(PlayerSfxEventType.StaminaSpent, PlayerSfxResourceKind.Stamina, -delta, current, maximum);
        }
    }

    private void HandlePlayerDied()
    {
        Emit(PlayerSfxEventType.PlayerDied);
    }

    private void HandleStatusApplied(PlayerStatusEffectType statusType)
    {
        EmitStatus(PlayerSfxEventType.StatusApplied, statusType, 0f);
    }

    private void HandleStatusRemoved(PlayerStatusEffectType statusType)
    {
        EmitStatus(PlayerSfxEventType.StatusRemoved, statusType, 0f);
    }

    private void HandleStatusDamageTick(PlayerStatusEffectType statusType, float damage)
    {
        EmitStatus(PlayerSfxEventType.StatusDamageTick, statusType, damage);
    }

    private void Emit(PlayerSfxEventType eventType)
    {
        Emit(CreateContext(eventType));
    }

    private void EmitResource(
        PlayerSfxEventType eventType,
        PlayerSfxResourceKind resourceKind,
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

    private void EmitStatus(PlayerSfxEventType eventType, PlayerStatusEffectType statusType, float amount)
    {
        Emit(CreateContext(
            eventType,
            PlayerSfxResourceKind.None,
            transform.position,
            Vector2.zero,
            amount,
            0f,
            0f,
            statusType,
            true));
    }

    private void Emit(in PlayerSfxEventContext context)
    {
        playerSfx?.HandleEvent(context);
    }

    private PlayerSfxEventContext CreateContext(PlayerSfxEventType eventType)
    {
        return CreateContext(
            eventType,
            PlayerSfxResourceKind.None,
            transform.position,
            Vector2.zero,
            0f,
            0f,
            0f,
            default,
            false);
    }

    private PlayerSfxEventContext CreateContext(
        PlayerSfxEventType eventType,
        PlayerSfxResourceKind resourceKind,
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

        return new PlayerSfxEventContext(
            playerSfx,
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
