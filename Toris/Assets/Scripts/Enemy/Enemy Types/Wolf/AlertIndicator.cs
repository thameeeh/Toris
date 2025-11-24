using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Enemy))]
public class EnemyAlertIndicator : MonoBehaviour
{
    [Header("Alert Indicator")]
    [SerializeField] private Vector3 indicatorOffset = new Vector3(0f, 1.25f, 0f);
    [SerializeField] private float displayDuration = 1.25f;
    [SerializeField] private Color indicatorColor = new Color(0.94f, 0.29f, 0.12f);
    [SerializeField] private string indicatorText = "!";
    [SerializeField] private float baseFontSize = 4f;
    [SerializeField] private float minFontSize = 2f;
    [SerializeField] private float maxFontSize = 6f;

    private Enemy _enemy;
    private GameObject _indicator;
    private TextMeshPro _indicatorText;
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
    }

    private void CreateIndicator()
    {
        if (_indicator != null) return;

        _indicator = new GameObject("AlertIndicator");
        _indicator.transform.SetParent(transform);
        _indicator.transform.localPosition = indicatorOffset;
        _indicator.transform.localRotation = Quaternion.identity;

        _indicatorText = _indicator.AddComponent<TextMeshPro>();
        _indicatorText.text = indicatorText;
        _indicatorText.alignment = TextAlignmentOptions.Center;
        _indicatorText.fontSize = baseFontSize;
        _indicatorText.enableAutoSizing = true;
        _indicatorText.fontSizeMin = minFontSize;
        _indicatorText.fontSizeMax = maxFontSize;
        _indicatorText.color = indicatorColor;
        _indicatorText.raycastTarget = false;

        var meshRenderer = _indicator.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.sortingLayerID = SortingLayer.NameToID("Default");
            meshRenderer.sortingOrder = 100;
        }

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

    private IEnumerator DisplayIndicatorRoutine()
    {
        if (_indicator == null)
        {
            yield break;
        }

        _indicator.SetActive(true);
        var waitDuration = Mathf.Max(0f, displayDuration);
        if (waitDuration > 0f)
        {
            yield return new WaitForSeconds(waitDuration);
        }
        else
        {
            yield return null;
        }
        _indicator.SetActive(false);
        _displayRoutine = null;
    }

    private void HideIndicator()
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
}