using UnityEngine;

[System.Serializable]
public sealed class ArrowRainRuntime : PlayerAbilityRuntime
{
    private bool _isActive;
    private float _activationEndTime;

    public bool IsActive => _isActive;
    public float ActivationEndTime => _activationEndTime;

    public void Activate(float duration)
    {
        _isActive = true;
        _activationEndTime = Time.time + Mathf.Max(0f, duration);
    }

    public void RefreshState()
    {
        if (_isActive && Time.time >= _activationEndTime)
        {
            _isActive = false;
            _activationEndTime = 0f;
        }
    }
}
