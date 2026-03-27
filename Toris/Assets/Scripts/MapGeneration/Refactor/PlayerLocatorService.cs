using UnityEngine;

public sealed class PlayerLocatorService : IPlayerLocator
{
    private readonly Transform fallbackPlayerTransform;

    public PlayerLocatorService(Transform fallbackPlayerTransform)
    {
        this.fallbackPlayerTransform = fallbackPlayerTransform;
    }

    public Transform GetPlayerTransform()
    {
        if (fallbackPlayerTransform != null)
            return fallbackPlayerTransform;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }
}