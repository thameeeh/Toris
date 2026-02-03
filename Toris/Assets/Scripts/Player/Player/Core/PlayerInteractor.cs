using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] private PlayerInputReader _inputReader;
    [SerializeField] private TileInteractor _tileInteractor;

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

        _tileInteractor?.HandleInteract();
    }

}
