using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyAlertIndicator : MonoBehaviour
{
    private const int FallbackSpriteWidth = 7;
    private const int FallbackSpriteHeight = 13;
    private const int TransparentPixelAlpha = 0;

    private static Sprite fallbackAlertSprite;

    [Header("Alert Indicator")]
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.45f, 0f);
    [SerializeField] private float displayDuration = 1.25f;
    [SerializeField] private Sprite alertSprite;
    [SerializeField] private Color indicatorColor = new Color(0.96f, 0.06f, 0.1f, 1f);
    [SerializeField] private Vector3 indicatorScale = Vector3.one * 0.5f;
    [SerializeField] private int sortingOrder = 100;

    private Enemy _enemy;
    private GameObject _indicator;
    private SpriteRenderer _indicatorRenderer;
    private Coroutine _displayRoutine;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        CreateIndicator();
    }

    private void OnEnable()
    {
        if (_enemy == null)
            TryGetComponent(out _enemy);

        if (_enemy != null)
        {
            _enemy.AggroStatusChanged += HandleAggroStatusChanged;
            _enemy.AlertTriggered += HandleAlertTriggered;
        }
    }

    private void OnDisable()
    {
        if (_enemy != null)
        {
            _enemy.AggroStatusChanged -= HandleAggroStatusChanged;
            _enemy.AlertTriggered -= HandleAlertTriggered;
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

        if (_indicator != null)
        {
            _indicator.SetActive(false);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        if (_indicatorRenderer != null)
        {
            _indicatorRenderer.sprite = ResolveAlertSprite();
            _indicatorRenderer.color = indicatorColor;
            _indicatorRenderer.sortingOrder = sortingOrder;
        }

        if (_indicator != null)
        {
            _indicator.transform.localPosition = indicatorOffset;
            _indicator.transform.localScale = indicatorScale;
        }
    }
#endif

    private void CreateIndicator()
    {
        if (_indicator != null) return;

        _indicator = new GameObject("AlertIndicator");
        _indicator.transform.SetParent(transform);
        _indicator.transform.localPosition = indicatorOffset;
        _indicator.transform.localRotation = Quaternion.identity;
        _indicator.transform.localScale = indicatorScale;

        _indicatorRenderer = _indicator.AddComponent<SpriteRenderer>();
        _indicatorRenderer.sprite = ResolveAlertSprite();
        _indicatorRenderer.color = indicatorColor;
        _indicatorRenderer.sortingLayerID = SortingLayer.NameToID("Default");
        _indicatorRenderer.sortingOrder = sortingOrder;

        _indicator.SetActive(false);
    }

    private void HandleAggroStatusChanged(bool isAggroed)
    {
        if (!isAggroed)
            HideIndicator();
    }

    private void HandleAlertTriggered(Enemy enemy, EnemyAlertReason reason)
    {
        ShowTimed();
    }

    public void ShowTimed()
    {
        if (_indicator == null)
        {
            CreateIndicator();
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
        }

        _displayRoutine = StartCoroutine(DisplayIndicatorRoutine());
    }

    public void ShowPersistent()
    {
        if (_indicator == null)
        {
            CreateIndicator();
        }

        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

        _indicator.SetActive(true);
    }

    public void HideIndicator()
    {
        if (_displayRoutine != null)
        {
            StopCoroutine(_displayRoutine);
            _displayRoutine = null;
        }

        if (_indicator != null)
        {
            _indicator.SetActive(false);
        }
    }

    private IEnumerator DisplayIndicatorRoutine()
    {
        if (_indicator == null)
        {
            yield break;
        }

        _indicator.SetActive(true);

        float waitDuration = Mathf.Max(0f, displayDuration);
        if (waitDuration > 0f)
        {
            yield return new WaitForSeconds(waitDuration);
        }

        _indicator.SetActive(false);
        _displayRoutine = null;
    }

    private Sprite ResolveAlertSprite()
    {
        return alertSprite != null ? alertSprite : FallbackAlertSprite;
    }

    private static Sprite FallbackAlertSprite
    {
        get
        {
            if (fallbackAlertSprite != null)
                return fallbackAlertSprite;

            Texture2D texture = new Texture2D(FallbackSpriteWidth, FallbackSpriteHeight, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };

            Color32 clear = new Color32(255, 255, 255, TransparentPixelAlpha);
            Color32 white = Color.white;

            for (int y = 0; y < FallbackSpriteHeight; y++)
            {
                for (int x = 0; x < FallbackSpriteWidth; x++)
                {
                    texture.SetPixel(x, y, clear);
                }
            }

            for (int y = 5; y <= 11; y++)
            {
                texture.SetPixel(3, y, white);
            }

            texture.SetPixel(3, 2, white);
            texture.SetPixel(3, 1, white);
            texture.Apply();

            fallbackAlertSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, FallbackSpriteWidth, FallbackSpriteHeight),
                new Vector2(0.5f, 0.5f),
                FallbackSpriteHeight);
            fallbackAlertSprite.hideFlags = HideFlags.HideAndDontSave;
            return fallbackAlertSprite;
        }
    }
}
