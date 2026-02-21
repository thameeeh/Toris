using UnityEngine;
using UnityEngine.Windows;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private PlayerInputReaderSO _inputReader;

    private IInteractable _current;

    private void OnEnable()
    {
        if (_inputReader != null)
            _inputReader.OnInteractPressed += HandleInteractPressed;
    }

    private void OnDisable()
    {
        if (_inputReader != null)
            _inputReader.OnInteractPressed -= HandleInteractPressed;

        _current = null;
    }
    private void OnValidate()
    {
        if (_inputReader == null)
        {
            Debug.LogError($"<b><color=red>[PlayerInteractor]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}<b>", this);
        }
    }

    public void SetCurrent(IInteractable interactable)
    {
        _current = interactable;
    }

    public void ClearCurrent(IInteractable interactable)
    {
        if (_current == interactable)
            _current = null;
    }

    private void HandleInteractPressed()
    {
        if (_current is UnityEngine.Object uobj && uobj == null)
            _current = null;

        if (_current != null)
        {
            _current.Interact(gameObject);
            return;
        }
    }
}
