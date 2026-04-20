using UnityEngine;

[System.Serializable]
public class PlayerAbilityRuntime
{
    private PlayerAbilitySO _definition;
    private float _nextReadyTime;

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