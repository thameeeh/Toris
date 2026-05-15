using OutlandHaven.Inventory;
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
    [SerializeField] private InventoryActionController inventoryActions;

    [Header("Movement Events")]
    [SerializeField] private float movementStartSpeed = 0.1f;

    [Header("Diagnostics")]
    [SerializeField] private bool debugLogSfxEvents;

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

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveDependencies();
        movementStartSpeed = Mathf.Max(0f, movementStartSpeed);
    }
#endif

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

        if (inventoryActions == null)
            TryGetComponent(out inventoryActions);
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
            stats.OnHealthResourceChanged += HandleHealthResourceChanged;
            stats.OnStaminaResourceChanged += HandleStaminaResourceChanged;
            stats.OnPlayerDied += HandlePlayerDied;
        }

        if (statusController != null)
        {
            statusController.OnStatusApplied += HandleStatusApplied;
            statusController.OnStatusRemoved += HandleStatusRemoved;
            statusController.OnStatusDamageTick += HandleStatusDamageTick;
        }

        if (inventoryActions != null)
        {
            inventoryActions.ConsumableUsed += HandleConsumableUsed;
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
            stats.OnHealthResourceChanged -= HandleHealthResourceChanged;
            stats.OnStaminaResourceChanged -= HandleStaminaResourceChanged;
            stats.OnPlayerDied -= HandlePlayerDied;
        }

        if (statusController != null)
        {
            statusController.OnStatusApplied -= HandleStatusApplied;
            statusController.OnStatusRemoved -= HandleStatusRemoved;
            statusController.OnStatusDamageTick -= HandleStatusDamageTick;
        }

        if (inventoryActions != null)
        {
            inventoryActions.ConsumableUsed -= HandleConsumableUsed;
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
            PlayerResourceChangeReason.Unknown,
            default,
            false));
    }

    private void HandleDashCompleted()
    {
        Emit(PlayerSfxEventType.DashCompleted);
    }

    private void HandleHealthResourceChanged(PlayerResourceChangeContext change)
    {
        float delta = hasHealthSnapshot ? change.Delta : 0f;
        previousHealth = change.CurrentValue;
        hasHealthSnapshot = true;

        if (!ShouldEmitResourceEvents(change.Reason))
            return;

        EmitResource(
            PlayerSfxEventType.HealthChanged,
            PlayerSfxResourceKind.Health,
            delta,
            change.CurrentValue,
            change.MaxValue,
            change.Reason);

        if (delta > 0f)
        {
            EmitResource(
                PlayerSfxEventType.Healed,
                PlayerSfxResourceKind.Health,
                delta,
                change.CurrentValue,
                change.MaxValue,
                change.Reason);
        }
        else if (delta < 0f)
        {
            float damageAmount = -delta;
            EmitResource(
                PlayerSfxEventType.Damaged,
                PlayerSfxResourceKind.Health,
                damageAmount,
                change.CurrentValue,
                change.MaxValue,
                change.Reason);
        }
    }

    private void HandleStaminaResourceChanged(PlayerResourceChangeContext change)
    {
        float delta = hasStaminaSnapshot ? change.Delta : 0f;
        previousStamina = change.CurrentValue;
        hasStaminaSnapshot = true;

        if (!ShouldEmitResourceEvents(change.Reason))
            return;

        EmitResource(
            PlayerSfxEventType.StaminaChanged,
            PlayerSfxResourceKind.Stamina,
            delta,
            change.CurrentValue,
            change.MaxValue,
            change.Reason);

        if (delta > 0f)
        {
            EmitResource(
                PlayerSfxEventType.StaminaRestored,
                PlayerSfxResourceKind.Stamina,
                delta,
                change.CurrentValue,
                change.MaxValue,
                change.Reason);
        }
        else if (delta < 0f)
        {
            EmitResource(
                PlayerSfxEventType.StaminaSpent,
                PlayerSfxResourceKind.Stamina,
                -delta,
                change.CurrentValue,
                change.MaxValue,
                change.Reason);
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

    private void HandleConsumableUsed(PlayerConsumableUseContext useContext)
    {
        EmitConsumable(PlayerSfxEventType.ConsumableUsed, useContext);

        if (useContext.EffectMode == ConsumableEffectMode.TimedPlayerEffect)
        {
            EmitConsumable(PlayerSfxEventType.TimedConsumableUsed, useContext);
            return;
        }

        switch (useContext.Payload)
        {
            case ConsumptionSlot.HP:
                EmitConsumable(PlayerSfxEventType.HealthConsumableUsed, useContext);
                break;
            case ConsumptionSlot.Mana:
                EmitConsumable(PlayerSfxEventType.ManaConsumableUsed, useContext);
                break;
        }
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
        float maximum,
        PlayerResourceChangeReason resourceChangeReason = PlayerResourceChangeReason.Unknown)
    {
        Emit(CreateContext(
            eventType,
            resourceKind,
            transform.position,
            Vector2.zero,
            amount,
            current,
            maximum,
            resourceChangeReason,
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
            PlayerResourceChangeReason.Unknown,
            statusType,
            true));
    }

    private void EmitConsumable(PlayerSfxEventType eventType, PlayerConsumableUseContext useContext)
    {
        PlayerSfxResourceKind resourceKind = ResolveConsumableResourceKind(useContext);
        float current = ResolveCurrentResourceValue(resourceKind);
        float maximum = ResolveMaxResourceValue(resourceKind);

        Emit(CreateContext(
            eventType,
            resourceKind,
            transform.position,
            Vector2.zero,
            useContext.Amount,
            current,
            maximum,
            ResolveConsumableResourceChangeReason(useContext),
            default,
            false));
    }

    private void Emit(in PlayerSfxEventContext context)
    {
        DebugLogEvent(context);

        if (playerSfx == null)
        {
            DebugLogMissingHub(context);
            return;
        }

        playerSfx.HandleEvent(context);
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
            PlayerResourceChangeReason.Unknown,
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
        PlayerResourceChangeReason resourceChangeReason,
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
            resourceChangeReason,
            statusType,
            hasStatusType);
    }

    private static PlayerSfxResourceKind ResolveConsumableResourceKind(PlayerConsumableUseContext useContext)
    {
        if (useContext.EffectMode != ConsumableEffectMode.InstantResource)
            return PlayerSfxResourceKind.None;

        return useContext.Payload switch
        {
            ConsumptionSlot.HP => PlayerSfxResourceKind.Health,
            ConsumptionSlot.Mana => PlayerSfxResourceKind.Stamina,
            _ => PlayerSfxResourceKind.None
        };
    }

    private static PlayerResourceChangeReason ResolveConsumableResourceChangeReason(PlayerConsumableUseContext useContext)
    {
        return useContext.EffectMode == ConsumableEffectMode.InstantResource
            ? PlayerResourceChangeReason.Restore
            : PlayerResourceChangeReason.Unknown;
    }

    private float ResolveCurrentResourceValue(PlayerSfxResourceKind resourceKind)
    {
        if (stats == null)
            return 0f;

        return resourceKind switch
        {
            PlayerSfxResourceKind.Health => stats.currentHP,
            PlayerSfxResourceKind.Stamina => stats.currentStamina,
            _ => 0f
        };
    }

    private float ResolveMaxResourceValue(PlayerSfxResourceKind resourceKind)
    {
        if (stats == null)
            return 0f;

        return resourceKind switch
        {
            PlayerSfxResourceKind.Health => stats.maxHP,
            PlayerSfxResourceKind.Stamina => stats.maxStamina,
            _ => 0f
        };
    }

    private static bool ShouldEmitResourceEvents(PlayerResourceChangeReason reason)
    {
        return reason switch
        {
            PlayerResourceChangeReason.Initialization => false,
            PlayerResourceChangeReason.RuntimeStateSync => false,
            PlayerResourceChangeReason.ResolvedEffectsChanged => false,
            _ => true
        };
    }

    private void DebugLogEvent(in PlayerSfxEventContext context)
    {
#if UNITY_EDITOR
        if (!debugLogSfxEvents)
            return;

        Debug.Log(
            $"[PlayerSfxEventBridge] Emitting {context.EventType}. hasHub={playerSfx != null}, hasAudio={context.HasAudio}, amount={context.Amount:0.###}, current={context.CurrentValue:0.###}, max={context.MaxValue:0.###}, world={context.WorldPosition}",
            this);
#endif
    }

    private void DebugLogMissingHub(in PlayerSfxEventContext context)
    {
#if UNITY_EDITOR
        if (!debugLogSfxEvents)
            return;

        Debug.LogWarning(
            $"[PlayerSfxEventBridge] Cannot dispatch {context.EventType}: PlayerSfx hub is missing.",
            this);
#endif
    }

}
