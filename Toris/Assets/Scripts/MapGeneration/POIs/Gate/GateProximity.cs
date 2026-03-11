using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GateProximity : MonoBehaviour
{
    private IInteractable _interactable;

    private void Awake() => _interactable = GetComponentInParent<IInteractable>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerInteractor>(out var pi)) pi.SetCurrent(_interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerInteractor>(out var pi)) pi.ClearCurrent(_interactable);
    }
}
