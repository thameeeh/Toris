using UnityEngine;

[System.Serializable]
public sealed class RamboBowRuntime : PlayerAbilityRuntime
{
    private bool _isHeld;
    private bool _isActive;
    private float _activationStartTime;
    private float _nextShotTime;

    public bool IsHeld => _isHeld;
    public bool IsActive => _isActive;
    public float ActivationStartTime => _activationStartTime;
    public float NextShotTime => _nextShotTime;


    public void SetHeld(bool isHeld)
    {
        _isHeld = isHeld;
    }

    public void Activate()
    {
        _isActive = true;
        _activationStartTime = Time.time;
        _nextShotTime = Time.time;
    }

    public void Deactivate()
    {
        _isHeld = false;
        _isActive = false;
    }

    public void ScheduleNextShot(float shotsPerSecond)
    {
        float safeShotsPerSecond = Mathf.Max(0.01f, shotsPerSecond);
        float shotInterval = 1f / safeShotsPerSecond;
        _nextShotTime = Time.time + shotInterval;
    }

    public bool HasReachedMaxDuration(float maxDuration)
    {
        return maxDuration > 0f && Time.time - _activationStartTime >= maxDuration;
    }

    public bool CanFireNow()
    {
        return Time.time >= _nextShotTime;
    }
}