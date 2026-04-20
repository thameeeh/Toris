using UnityEngine;

[System.Serializable]
public class PlayerAbilityRuntime
{
    private PlayerAbilitySO _definition;
    private float _nextReadyTime;
    private float _bowDrawBlockedUntilTime;
    private float _movementBlockedUntilTime;

    public PlayerAbilitySO Definition => _definition;
    public bool HasAbility => _definition != null;
    public bool IsOnCooldown => Time.time < _nextReadyTime;
    public float CooldownRemaining => Mathf.Max(0f, _nextReadyTime - Time.time);

    public void Initialize(PlayerAbilitySO definition)
    {
        _definition = definition;
        ResetRuntimeState();
    }

    public void ResetRuntimeState()
    {
        _nextReadyTime = 0f;
        _bowDrawBlockedUntilTime = 0f;
        _movementBlockedUntilTime = 0f;
    }

    public bool IsUnlocked(PlayerAbilityContext context)
    {
        return _definition != null && _definition.IsUnlocked(context);
    }

    public bool IsReady(PlayerAbilityContext context)
    {
        return _definition != null && !IsOnCooldown && IsUnlocked(context);
    }

    public void StartCooldown()
    {
        if (_definition == null)
            return;

        _nextReadyTime = Time.time + _definition.cooldownSeconds;
    }

    public void BlockBowDraw()
    {
        if (_definition == null || !_definition.blocksBowDraw)
            return;

        BlockBowDraw(_definition.bowDrawLockDuration);
    }

    public void BeginBowUse(PlayerAbilityContext context)
    {
        if (_definition == null || !_definition.blocksBowDraw)
            return;

        context.bow?.CancelCurrentDraw(_definition.abilityName);
        BlockBowDraw();
    }

    public void BeginMovementUse(PlayerAbilityContext context)
    {
        if (_definition == null || !_definition.blocksMovement)
            return;

        context.motor?.StopMovementImmediately();
        BlockMovement();
    }

    public void BeginAbilityUse(PlayerAbilityContext context)
    {
        BeginBowUse(context);
        BeginMovementUse(context);
    }

    public void BlockBowDraw(float duration)
    {
        _bowDrawBlockedUntilTime = Mathf.Max(_bowDrawBlockedUntilTime, Time.time + Mathf.Max(0f, duration));
    }

    public void BlockMovement()
    {
        if (_definition == null || !_definition.blocksMovement)
            return;

        BlockMovement(_definition.movementLockDuration);
    }

    public void BlockMovement(float duration)
    {
        _movementBlockedUntilTime = Mathf.Max(_movementBlockedUntilTime, Time.time + Mathf.Max(0f, duration));
    }

    public virtual bool IsBlockingBowDraw(PlayerAbilityContext context)
    {
        return _definition != null
            && _definition.blocksBowDraw
            && Time.time < _bowDrawBlockedUntilTime;
    }

    public virtual bool IsBlockingMovement(PlayerAbilityContext context)
    {
        return _definition != null
            && _definition.blocksMovement
            && Time.time < _movementBlockedUntilTime;
    }

    public void OnButtonDown(PlayerAbilityContext context)
    {
        _definition?.OnButtonDown(this, context);
    }

    public void OnButtonUp(PlayerAbilityContext context)
    {
        _definition?.OnButtonUp(this, context);
    }

    public void Tick(PlayerAbilityContext context)
    {
        _definition?.Tick(this, context);
    }
}
