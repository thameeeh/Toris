using UnityEngine;

public class ArrowRainVisual : MonoBehaviour, IPoolable
{
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _travelDuration;
    private float _elapsed;
    private bool _isActive;
    private PooledVisualInstance _pooledVisualInstance;

    public void Initialize(Vector3 startPosition, Vector3 targetPosition, float travelDurationSeconds)
    {
        if (_pooledVisualInstance == null)
            TryGetComponent(out _pooledVisualInstance);

        _startPosition = startPosition;
        _targetPosition = targetPosition;
        _travelDuration = Mathf.Max(0.01f, travelDurationSeconds);
        _elapsed = 0f;
        _isActive = true;
        transform.position = _startPosition;
    }

    public void OnSpawned()
    {
        if (_pooledVisualInstance == null)
            TryGetComponent(out _pooledVisualInstance);

        _elapsed = 0f;
        _isActive = false;
    }

    public void OnDespawned()
    {
        _elapsed = 0f;
        _isActive = false;
    }

    private void Update()
    {
        if (!_isActive)
            return;

        _elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(_elapsed / _travelDuration);
        transform.position = Vector3.Lerp(_startPosition, _targetPosition, progress);

        if (progress >= 1f)
        {
            _isActive = false;

            if (_pooledVisualInstance != null)
                _pooledVisualInstance.Despawn();
            else
                Destroy(gameObject);
        }
    }
}
