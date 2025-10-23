using System;
using UnityEngine;

[Serializable]
public class DashAbility : IMovementAbility
{
    [SerializeField] private DashConfig _config;

    private Rigidbody2D _body;
    private PlayerMoveConfig _moveConfig;
    private Action<Vector2> _applyVelocity;

    private Vector2 _dashDirection;
    private float _activeTimeRemaining;
    private float _activeTimeElapsed;
    private float _cooldownTimer;

    public DashConfig Config => _config;
    public bool isActive => _activeTimeRemaining > 0f;
    public bool isOnCooldown => _cooldownTimer > 0f;

    public event Action Activated;
    public event Action Completed;

    public void Initialize(Rigidbody2D body, PlayerMoveConfig moveConfig, Action<Vector2> applyVelocity)
    {
        _body = body;
        _moveConfig = moveConfig;
        _applyVelocity = applyVelocity;
    }

    public bool TryActivate(Vector2 direction)
    {
        if (_config == null || _body == null || _applyVelocity == null)
            return false;
        if (isActive || isOnCooldown)
            return false;
        if (direction.sqrMagnitude < 0.01f)
            return false;

        _dashDirection = direction.normalized;
        _activeTimeRemaining = _config.duration;
        _activeTimeElapsed = 0f;
        Activated?.Invoke();
        return true;
    }

    public void FixedTick(float deltaTime)
    {
        if (_config == null || _body == null || _applyVelocity == null)
            return;

        if (isActive)
        {
            float duration = Mathf.Max(_config.duration, Mathf.Epsilon);
            _activeTimeElapsed += deltaTime;
            _activeTimeRemaining = Mathf.Max(0f, _activeTimeRemaining - deltaTime);

            float normalizedTime = Mathf.Clamp01(_activeTimeElapsed / duration);
            float runSpeed = _moveConfig ? _moveConfig.speed : 0f;
            float blendTarget = Mathf.Lerp(0f, runSpeed, _config.blendToRun);
            float speedBlend = Mathf.Lerp(_config.initialSpeed, blendTarget, normalizedTime);
            float shapedSpeed = speedBlend * _config.speedCurve.Evaluate(normalizedTime);

            _applyVelocity(_dashDirection * shapedSpeed);

            if (!isActive)
            {
                _cooldownTimer = _config.cooldown;
                Completed?.Invoke();
            }

            return;
        }

        if (isOnCooldown)
        {
            _cooldownTimer = Mathf.Max(0f, _cooldownTimer - deltaTime);
        }
    }

    public void Cancel()
    {
        bool wasActive = isActive;
        _activeTimeElapsed = 0f;
        _activeTimeRemaining = 0f;
        if (wasActive)
            Completed?.Invoke();
    }
}