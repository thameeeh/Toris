using OutlandHaven.UIToolkit;
using UnityEngine;

public interface IContainerInteractable
{

    // Tries to add Item to the target container
    // Destroys itself on success (Item)
    bool Interact(InventoryContainerSO targetContainer);

    Vector3 InteractionPosition { get; }
    string GetInteractionPrompt();
}
