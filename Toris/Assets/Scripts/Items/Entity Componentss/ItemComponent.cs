using UnityEngine;
using OutlandHaven.UIToolkit;
using System;

[System.Serializable]
public abstract class ItemComponent
{
    // By default, components don't have a runtime state (e.g., a simple IconComponent).
    // Override this only in components that need dynamic tracking.
    public virtual ItemComponentState CreateInitialState()
    {
        return null;
    }
}
