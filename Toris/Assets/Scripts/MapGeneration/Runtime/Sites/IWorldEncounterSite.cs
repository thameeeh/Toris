using System;
using UnityEngine;

public interface IWorldEncounterSite
{
    bool IsInitialized { get; }
    bool IsCleared { get; }
    Vector3 WorldPosition { get; }

    event Action Initialized;
    event Action Cleared;
    event Action<Vector3> DamagedAlert;
}
