using OutlandHaven.UIToolkit;
using UnityEngine;

public interface IContainerInteractable
{
    // Returns true if interaction was successful (so you can destroy the object, etc.)
    bool Interact(InventoryContainerSO targetContainer);
    string GetInteractionPrompt(); // e.g., "Press E to Pick Up"
}
