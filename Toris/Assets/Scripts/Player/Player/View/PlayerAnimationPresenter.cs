using UnityEngine;

// PURPOSE:
// - Sole presentation bridge between gameplay systems and PlayerAnimationController
// - Reads gameplay-owned state and events
// - Keeps animation separate from gameplay logic

public class PlayerAnimationPresenter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAnimationController _animationController;
    [SerializeField] private PlayerMotor _motor;
    [SerializeField] private PlayerBowController _bowController;
    [SerializeField] private PlayerDamageReceiver _damageReceiver;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerFacing _playerFacing;

    private void LogShoot(string message)
    {
        PlayerShootDebug.Log(this, "AnimPresenter", message);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }

    private void OnEnable()
    {
        if (_motor != null)
        {
            _motor.DashStarted += HandleDashStarted;
        }

        if (_bowController != null)
        {
            _bowController.DrawStarted += HandleDrawStarted;
            _bowController.ShootReady += HandleShootReady;
            _bowController.ShotReleased += HandleShotReleased;
            _bowController.DryReleased += HandleDryReleased;
            _bowController.AbilityReleaseRequested += HandleAbilityReleaseRequested;
        }

        if (_damageReceiver != null)
        {
            _damageReceiver.OnHurtReceived += HandleHurtReceived;
        }

        if (_playerStats != null)
        {
            _playerStats.OnPlayerDied += HandlePlayerDied;
        }
    }

    private void OnDisable()
    {
        if (_motor != null)
        {
            _motor.DashStarted -= HandleDashStarted;
        }

        if (_bowController != null)
        {
            _bowController.DrawStarted -= HandleDrawStarted;
            _bowController.ShootReady -= HandleShootReady;
            _bowController.ShotReleased -= HandleShotReleased;
            _bowController.DryReleased -= HandleDryReleased;
            _bowController.AbilityReleaseRequested -= HandleAbilityReleaseRequested;
        }

        if (_damageReceiver != null)
        {
            _damageReceiver.OnHurtReceived -= HandleHurtReceived;
        }

        if (_playerStats != null)
        {
            _playerStats.OnPlayerDied -= HandlePlayerDied;
        }
    }

    private void Update()
    {
        if (_animationController == null || _motor == null)
            return;

        Vector2 animationMoveInput = _motor.isDashing ? Vector2.zero : _motor.CurrentMoveInput;
        _animationController.Tick(animationMoveInput);

        if (_bowController != null && _bowController.IsDrawing)
        {
            Vector2 aim = _bowController.CurrentAimDirection;
            if (aim.sqrMagnitude > 0.0001f)
            {
                _animationController.UpdateAim(aim);
            }

            return;
        }

        if (_playerFacing != null && _playerFacing.CurrentFacing.sqrMagnitude > 0.0001f)
        {
            _animationController.UpdateAim(_playerFacing.CurrentFacing);
        }
    }

    private void HandleDashStarted(Vector2 dashDirection)
    {
        LogShoot($"DashStarted received. dir={FormatVector(dashDirection)} bowDrawing={(_bowController != null && _bowController.IsDrawing)}");
        if (_bowController != null && _bowController.CancelCurrentDraw("DashStarted"))
        {
            LogShoot("DashStarted canceled active bow draw.");
        }

        if (_animationController == null)
            return;

        _animationController.PlayDash(dashDirection);
    }

    private void HandleDrawStarted()
    {
        float readyDuration = _bowController != null && _bowController.BowConfig != null
            ? _bowController.BowConfig.nockTime
            : 0f;
        Vector2 initialAim = Vector2.zero;

        if (_bowController != null)
        {
            initialAim = _bowController.CurrentAimDirection;
        }
        else if (_playerFacing != null)
        {
            initialAim = _playerFacing.CurrentFacing;
        }

        LogShoot($"DrawStarted received. readyDuration={readyDuration:F3} initialAim={FormatVector(initialAim)}");
        _animationController?.BeginShoot(readyDuration, initialAim);
    }

    private void HandleShootReady()
    {
        LogShoot("ShootReady received.");
        _animationController?.EnterShootHold();
    }

    private void HandleShotReleased()
    {
        LogShoot("ShotReleased received.");
        _animationController?.ReleaseShoot();
    }

    private void HandleDryReleased()
    {
        LogShoot("DryReleased received.");
        _animationController?.CancelShoot();
    }

    private void HandleAbilityReleaseRequested(Vector2 shotDirection)
    {
        LogShoot($"AbilityReleaseRequested received. dir={FormatVector(shotDirection)}");
        _animationController?.PlayAbilityShootRelease(shotDirection);
    }

    private void HandleHurtReceived()
    {
        LogShoot($"HurtReceived. bowDrawing={(_bowController != null && _bowController.IsDrawing)}");
        if (_bowController != null && _bowController.CancelCurrentDraw("HurtReceived"))
        {
            LogShoot("HurtReceived canceled active bow draw.");
        }

        if (_animationController == null)
            return;

        _animationController?.PlayHurt();
    }

    private void HandlePlayerDied()
    {
        LogShoot("PlayerDied received.");
        if (_bowController != null && _bowController.CancelCurrentDraw("PlayerDied"))
        {
            LogShoot("PlayerDied canceled active bow draw.");
        }

        if (_animationController == null)
            return;

        _animationController?.PlayDeath();
    }
}
