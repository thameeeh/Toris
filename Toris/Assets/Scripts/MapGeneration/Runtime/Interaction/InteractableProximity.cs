using UnityEngine;

/// <summary>
/// Generic proximity trigger that makes any parent IInteractable usable by PlayerInteractor.
/// Use this for non-NPC interactables such as job boards, gates, signs, and world objects.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class InteractableProximity : MonoBehaviour
{
    private IInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponentInParent<IInteractable>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_interactable != null && TryResolvePlayerInteractor(other, out PlayerInteractor playerInteractor))
            playerInteractor.SetCurrent(_interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (_interactable != null && TryResolvePlayerInteractor(other, out PlayerInteractor playerInteractor))
            playerInteractor.ClearCurrent(_interactable);
    }

    private static bool TryResolvePlayerInteractor(Collider2D other, out PlayerInteractor playerInteractor)
    {
        if (other.TryGetComponent(out playerInteractor))
            return true;

        playerInteractor = other.GetComponentInParent<PlayerInteractor>();
        return playerInteractor != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        if (triggerCollider != null && !triggerCollider.isTrigger)
            Debug.LogWarning("[InteractableProximity] Collider2D should usually be marked Is Trigger.", this);

        if (GetComponentInParent<IInteractable>() == null)
            Debug.LogWarning("[InteractableProximity] No IInteractable found in parent hierarchy.", this);
    }
#endif
}
