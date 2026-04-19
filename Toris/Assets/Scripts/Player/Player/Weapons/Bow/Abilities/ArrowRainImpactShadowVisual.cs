using UnityEngine;

public class ArrowRainImpactShadowVisual : MonoBehaviour, IPoolable
{
    private SpriteRenderer[] _spriteRenderers;
    private Color[] _baseColors;
    private Vector3 _baseScale;
    private PooledVisualInstance _pooledVisualInstance;
    private float _startTime;
    private float _duration;
    private float _startScaleMultiplier;
    private float _endScaleMultiplier;
    private float _startAlpha;
    private float _endAlpha;
    private bool _isInitialized;

    public void Initialize(
        float duration,
        float startScaleMultiplier,
        float endScaleMultiplier,
        float startAlpha,
        float endAlpha)
    {
        CacheVisualReferences();
        _startTime = Time.time;
        _duration = Mathf.Max(0.01f, duration);
        _startScaleMultiplier = Mathf.Max(0f, startScaleMultiplier);
        _endScaleMultiplier = Mathf.Max(0f, endScaleMultiplier);
        _startAlpha = Mathf.Clamp01(startAlpha);
        _endAlpha = Mathf.Clamp01(endAlpha);
        _isInitialized = true;

        ApplyProgress(0f);
    }

    public void OnSpawned()
    {
        CacheVisualReferences();
        ResetVisualState();
    }

    public void OnDespawned()
    {
        _isInitialized = false;
        ResetVisualState();
    }

    private void Update()
    {
        if (!_isInitialized)
            return;

        float progress = Mathf.Clamp01((Time.time - _startTime) / _duration);
        ApplyProgress(progress);

        if (progress >= 1f)
        {
            if (_pooledVisualInstance != null)
                _pooledVisualInstance.Despawn();
            else
                Destroy(gameObject);
        }
    }

    private void ApplyProgress(float progress)
    {
        float scaleMultiplier = Mathf.Lerp(_startScaleMultiplier, _endScaleMultiplier, progress);
        transform.localScale = _baseScale * scaleMultiplier;

        float alphaMultiplier = Mathf.Lerp(_startAlpha, _endAlpha, progress);
        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            Color color = _baseColors[i];
            color.a = _baseColors[i].a * alphaMultiplier;
            spriteRenderer.color = color;
        }
    }

    private void CacheVisualReferences()
    {
        if (_pooledVisualInstance == null)
            TryGetComponent(out _pooledVisualInstance);

        if (_spriteRenderers != null && _spriteRenderers.Length > 0)
            return;

        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_spriteRenderers.Length];
        for (int i = 0; i < _spriteRenderers.Length; i++)
            _baseColors[i] = _spriteRenderers[i].color;

        _baseScale = transform.localScale;
    }

    private void ResetVisualState()
    {
        transform.localScale = _baseScale;

        if (_spriteRenderers == null || _baseColors == null)
            return;

        for (int i = 0; i < _spriteRenderers.Length; i++)
        {
            SpriteRenderer spriteRenderer = _spriteRenderers[i];
            if (spriteRenderer == null)
                continue;

            spriteRenderer.color = _baseColors[i];
        }
    }
}
