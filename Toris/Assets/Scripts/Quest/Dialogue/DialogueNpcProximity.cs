using UnityEngine;

/// <summary>
/// Mirrors the existing Toris proximity pattern used by gates:
/// when the player enters the trigger, this NPC becomes the current interactable.
/// Keeping this separate from the interactable lets the root NPC stay focused on dialogue selection.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DialogueNpcProximity : MonoBehaviour
{
    private IInteractable _interactable;

    private void Awake()
    {
        _interactable = GetComponentInParent<IInteractable>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (TryResolvePlayerInteractor(other, out var playerInteractor))
            playerInteractor.SetCurrent(_interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (TryResolvePlayerInteractor(other, out var playerInteractor))
            playerInteractor.ClearCurrent(_interactable);
    }

    private bool TryResolvePlayerInteractor(Collider2D other, out PlayerInteractor playerInteractor)
    {
        if (other.TryGetComponent(out playerInteractor))
            return true;

        playerInteractor = other.GetComponentInParent<PlayerInteractor>();
        return playerInteractor != null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (GetComponentInParent<IInteractable>() == null)
            Debug.LogWarning("[DialogueNpcProximity] No IInteractable found in parent hierarchy.", this);
    }
#endif
}
