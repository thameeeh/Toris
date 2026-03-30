using System.Collections.Generic;
using UnityEngine;

public sealed class WorldFeatureOwnershipGroup
{
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly List<GameObject> ownedInstances = new List<GameObject>();

    public Transform Root { get; }

    public IReadOnlyList<GameObject> OwnedInstances => ownedInstances;
    public int InstanceCount => ownedInstances.Count;

    public WorldFeatureOwnershipGroup(
        Transform root,
        WorldPoiPoolManager poiPoolManager)
    {
        Root = root;
        this.poiPoolManager = poiPoolManager;
    }

    public void AddInstance(GameObject instance)
    {
        if (instance != null)
            ownedInstances.Add(instance);
    }

    public void ClearInstances()
    {
        for (int i = 0; i < ownedInstances.Count; i++)
        {
            GameObject instance = ownedInstances[i];
            if (instance == null)
                continue;

            if (poiPoolManager != null)
                poiPoolManager.Release(instance);
            else
                Object.Destroy(instance);
        }

        ownedInstances.Clear();
    }
}