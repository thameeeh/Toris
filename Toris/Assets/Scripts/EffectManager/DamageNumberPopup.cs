using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DamageNumberPopup : MonoBehaviour, IEffectParametersReceiver, IEffectPoolListener
{
    private const float MinimumDurationSeconds = 0.01f;

    [Header("Text")]
    [SerializeField] private TextMeshPro text;
    [SerializeField] private TMP_FontAsset fontAsset;
    [SerializeField, Min(0.1f)] private float fontSize = 6f;
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 100;

    [Header("Animation")]
    [SerializeField, Min(MinimumDurationSeconds)] private float durationSeconds = 0.65f;
    [SerializeField] private Vector3 initialWorldOffset = new Vector3(0f, 0.12f, 0f);
    [SerializeField, Min(0f)] private float riseDistance = 0.36f;
    [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.35f;
    [SerializeField, Min(0f)] private float horizontalSpread = 0.04f;

    private EffectInstancePool _pooledInstance;
    private Vector3 _startPosition;
    private Color _displayColor = Color.white;
    private float _horizontalOffset;
    private float _elapsedSeconds;
    private bool _hasConfiguredText;
    private bool _isPlaying;

    private void Awake()
    {
        EnsureText();
    }

    private void Update()
    {
        if (!_isPlaying)
            return;

        _elapsedSeconds += Time.unscaledDeltaTime;

        float safeDuration = Mathf.Max(MinimumDurationSeconds, durationSeconds);
        float normalizedTime = Mathf.Clamp01(_elapsedSeconds / safeDuration);
        float easedRise = 1f - ((1f - normalizedTime) * (1f - normalizedTime));
        float horizontalTravel = _horizontalOffset * normalizedTime;

        transform.position = _startPosition
            + (Vector3.up * riseDistance * easedRise)
            + (Vector3.right * horizontalTravel);

        float fadeTime = Mathf.InverseLerp(fadeStartNormalized, 1f, normalizedTime);
        SetTextAlpha(1f - fadeTime);

        if (normalizedTime < 1f)
            return;

        _isPlaying = false;
        _pooledInstance?.OnEffectFinished();
    }

    public void ApplyEffectParameters(EffectVariant variant, float magnitude)
    {
        EnsureText();

        _displayColor = variant.ColorOverride;
        text.SetText("{0:0}", Mathf.RoundToInt(Mathf.Max(0f, magnitude)));
        text.color = _displayColor;
    }

    public void OnEffectSpawned()
    {
        EnsureText();

        if (_pooledInstance == null)
            TryGetComponent(out _pooledInstance);

        _startPosition = transform.position + initialWorldOffset;
        transform.position = _startPosition;
        _horizontalOffset = Random.Range(-horizontalSpread, horizontalSpread);
        _elapsedSeconds = 0f;
        _isPlaying = true;
        SetTextAlpha(1f);
    }

    public void OnEffectReleased()
    {
        _isPlaying = false;
        _elapsedSeconds = 0f;
        _horizontalOffset = 0f;

        if (text == null)
            return;

        text.SetText(string.Empty);
        SetTextAlpha(0f);
    }

    private void EnsureText()
    {
        if (text == null && !TryGetComponent(out text))
            text = gameObject.AddComponent<TextMeshPro>();

        if (_hasConfiguredText)
            return;

        if (fontAsset != null)
            text.font = fontAsset;

        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.richText = false;

        if (TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }

        _hasConfiguredText = true;
    }

    private void SetTextAlpha(float alpha)
    {
        if (text == null)
            return;

        Color color = _displayColor;
        color.a *= Mathf.Clamp01(alpha);
        text.color = color;
    }
}
