using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyAlertIndicator : MonoBehaviour
{
    [Header("Alert Indicator")]
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.25f, 0f);
    [SerializeField] private float displayDuration = 1.25f;
    [SerializeField] private Sprite alertSprite;
    [SerializeField] private Color indicatorColor = Color.white;
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
        if (_enemy != null)
        {
            _enemy.AggroStatusChanged += HandleAggroStatusChanged;
        }
    }

    private void OnDisable()
    {
        if (_enemy != null)
        {
            _enemy.AggroStatusChanged -= HandleAggroStatusChanged;
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

    private void CreateIndicator()
    {
        if (_indicator != null) return;

        _indicator = new GameObject("AlertIndicator");
        _indicator.transform.SetParent(transform);
        _indicator.transform.localPosition = indicatorOffset;
        _indicator.transform.localRotation = Quaternion.identity;
        _indicator.transform.localScale = indicatorScale;

        _indicatorRenderer = _indicator.AddComponent<SpriteRenderer>();
        _indicatorRenderer.sprite = alertSprite;
        _indicatorRenderer.color = indicatorColor;
        _indicatorRenderer.sortingLayerID = SortingLayer.NameToID("Default");
        _indicatorRenderer.sortingOrder = sortingOrder;

        _indicator.SetActive(false);
    }

    private void HandleAggroStatusChanged(bool isAggroed)
    {
        if (_indicator == null)
        {
            CreateIndicator();
        }

        if (!isAggroed)
        {
            HideIndicator();
            return;
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
}