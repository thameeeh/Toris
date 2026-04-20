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

    private void OnEnable()
    {
        if (_animationController != null)
        {
            _animationController.ShootNockReached += HandleShootNockReached;
        }

        if (_motor != null)
        {
            _motor.DashStarted += HandleDashStarted;
        }

        if (_bowController != null)
        {
            _bowController.DrawStarted += HandleDrawStarted;
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
        if (_animationController != null)
        {
            _animationController.ShootNockReached -= HandleShootNockReached;
        }

        if (_motor != null)
        {
            _motor.DashStarted -= HandleDashStarted;
        }

        if (_bowController != null)
        {
            _bowController.DrawStarted -= HandleDrawStarted;
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
        if (_animationController == null)
            return;

        _animationController.PlayDash(dashDirection);
    }

    private void HandleDrawStarted()
    {
        _animationController?.BeginShoot();
    }

    private void HandleShotReleased()
    {
        _animationController?.ReleaseShoot();
    }

    private void HandleDryReleased()
    {
        _animationController?.CancelShoot();
    }

    private void HandleShootNockReached()
    {
        _bowController?.NotifyNockReached();
    }

    private void HandleAbilityReleaseRequested(Vector2 shotDirection, bool useShortVariant)
    {
        _animationController?.PlayAbilityShootRelease(shotDirection, useShortVariant);
    }

    private void HandleHurtReceived()
    {
        _animationController?.PlayHurt();
    }

    private void HandlePlayerDied()
    {
        _animationController?.PlayDeath();
    }
}
