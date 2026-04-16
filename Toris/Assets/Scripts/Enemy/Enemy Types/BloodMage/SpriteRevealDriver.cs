using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteRevealDriver : MonoBehaviour
{
    private const string DefaultRevealPropertyName = "_Reveal";

    [Header("Shader")]
    [SerializeField] private string revealPropertyName = DefaultRevealPropertyName;

    [Header("Defaults")]
    [SerializeField, Min(0f)] private float defaultRevealInDuration = 0.35f;

    private SpriteRenderer _spriteRenderer;
    private Material _runtimeMaterialInstance;
    private int _revealPropertyId;
    private float _currentRevealValue;
    private float _startRevealValue;
    private float _targetRevealValue;
    private float _revealDuration;
    private float _revealElapsedTime;
    private bool _isRevealAnimating;
    private bool _hasRevealProperty;

    public float CurrentRevealValue => _currentRevealValue;
    public bool HasRevealProperty => _hasRevealProperty;

    private void Awake()
    {
        TryGetComponent(out _spriteRenderer);
        _revealPropertyId = Shader.PropertyToID(revealPropertyName);
        EnsureMaterialInstance();

        // Summon-driven visuals should start hidden until their owner explicitly reveals them.
        SetRevealImmediate(0f);
    }

    private void OnDestroy()
    {
        if (_runtimeMaterialInstance != null)
            Destroy(_runtimeMaterialInstance);
    }

    private void Update()
    {
        if (!_isRevealAnimating || !_hasRevealProperty)
            return;

        _revealElapsedTime += Time.deltaTime;
        float t = _revealDuration <= 0f ? 1f : Mathf.Clamp01(_revealElapsedTime / _revealDuration);
        _currentRevealValue = Mathf.Lerp(_startRevealValue, _targetRevealValue, t);
        ApplyRevealValue();

        if (t >= 1f)
            _isRevealAnimating = false;
    }

    public void SetRevealImmediate(float revealValue)
    {
        _currentRevealValue = Mathf.Clamp01(revealValue);
        _isRevealAnimating = false;
        ApplyRevealValue();
    }

    public void PlayRevealIn()
    {
        PlayReveal(1f, defaultRevealInDuration);
    }

    public void PlayRevealIn(float duration)
    {
        PlayReveal(1f, duration);
    }

    private void PlayReveal(float targetRevealValue, float duration)
    {
        if (!_hasRevealProperty)
        {
#if UNITY_EDITOR
            Debug.LogWarning(
                $"[SpriteRevealDriver:{name}] PlayReveal skipped because property '{revealPropertyName}' was not found on the runtime material.",
                this);
#endif
            return;
        }

        _startRevealValue = _currentRevealValue;
        _targetRevealValue = Mathf.Clamp01(targetRevealValue);
        _revealDuration = Mathf.Max(0f, duration);
        _revealElapsedTime = 0f;

        if (_revealDuration <= 0f)
        {
            SetRevealImmediate(_targetRevealValue);
            return;
        }

        _isRevealAnimating = true;
    }

    private void EnsureMaterialInstance()
    {
        if (_spriteRenderer == null)
            return;

        Material sharedMaterial = _spriteRenderer.sharedMaterial;
        if (sharedMaterial == null)
            return;

        _runtimeMaterialInstance = new Material(sharedMaterial)
        {
            name = $"{sharedMaterial.name} (Instance)"
        };

        _spriteRenderer.material = _runtimeMaterialInstance;
        _hasRevealProperty = _runtimeMaterialInstance.HasProperty(_revealPropertyId);
    }

    private void ApplyRevealValue()
    {
        if (!_hasRevealProperty || _runtimeMaterialInstance == null)
            return;

        _runtimeMaterialInstance.SetFloat(_revealPropertyId, _currentRevealValue);
    }
}
