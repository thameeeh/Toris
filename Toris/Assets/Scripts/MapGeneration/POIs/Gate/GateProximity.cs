using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class GateProximity : MonoBehaviour
{
    private IInteractable _interactable;

    private void Awake() => _interactable = GetComponentInChildren<IInteractable>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        var pi = other.GetComponent<PlayerInteractor>();
        if (pi != null) pi.SetCurrent(_interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var pi = other.GetComponent<PlayerInteractor>();
        if (pi != null) pi.ClearCurrent(_interactable);
    }
}
