using UnityEngine;

public sealed class PlayerLocatorService : IPlayerLocator
{
    private readonly Transform playerTransform;

    public PlayerLocatorService(Transform playerTransform)
    {
        this.playerTransform = playerTransform;
    }

    public Transform GetPlayerTransform()
    {
        return playerTransform;
    }
}