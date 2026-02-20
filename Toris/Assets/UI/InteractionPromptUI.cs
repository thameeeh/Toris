using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _promptText;
    [SerializeField] private GameObject _uiPanel;

    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
        _uiPanel.SetActive(false);
    }

    public void DisplayPrompt(string text, Vector3 worldPosition)
    {
        if (!_uiPanel.activeSelf) _uiPanel.SetActive(true);

        _promptText.text = text;

        Vector3 screenPos = _mainCam.WorldToScreenPoint(worldPosition);
        transform.position = screenPos;
    }

    public void Hide()
    {
        if (_uiPanel.activeSelf) _uiPanel.SetActive(false);
    }
}