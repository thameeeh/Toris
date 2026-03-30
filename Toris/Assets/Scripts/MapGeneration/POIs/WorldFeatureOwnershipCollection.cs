using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WorldFeatureOwnershipCollection<TKey>
{
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly string rootContainerName;
    private readonly Func<TKey, string> childRootNameFactory;

    private readonly Dictionary<TKey, WorldFeatureOwnershipGroup> groups =
        new Dictionary<TKey, WorldFeatureOwnershipGroup>();

    private Transform rootContainer;

    public WorldFeatureOwnershipCollection(
        WorldPoiPoolManager poiPoolManager,
        string rootContainerName,
        Func<TKey, string> childRootNameFactory)
    {
        this.poiPoolManager = poiPoolManager;
        this.rootContainerName = rootContainerName;
        this.childRootNameFactory = childRootNameFactory;
    }

    public bool ContainsKey(TKey key)
    {
        return groups.ContainsKey(key);
    }

    public bool TryGetGroup(TKey key, out WorldFeatureOwnershipGroup group)
    {
        return groups.TryGetValue(key, out group);
    }

    public WorldFeatureOwnershipGroup GetOrCreateGroup(TKey key)
    {
        if (groups.TryGetValue(key, out WorldFeatureOwnershipGroup existingGroup))
            return existingGroup;

        EnsureRootContainer();

        string childRootName = childRootNameFactory != null
            ? childRootNameFactory(key)
            : "WorldFeatureGroup";

        GameObject childRootObject = new GameObject(childRootName);
        Transform childRoot = childRootObject.transform;
        childRoot.SetParent(rootContainer, false);

        WorldFeatureOwnershipGroup group = new WorldFeatureOwnershipGroup(
            childRoot,
            poiPoolManager);

        groups.Add(key, group);
        return group;
    }

    public void RemoveAndClearGroup(TKey key)
    {
        if (!groups.TryGetValue(key, out WorldFeatureOwnershipGroup group))
            return;

        group.ClearInstances();

        if (group.Root != null)
            UnityEngine.Object.Destroy(group.Root.gameObject);

        groups.Remove(key);

        if (groups.Count == 0 && rootContainer != null)
        {
            UnityEngine.Object.Destroy(rootContainer.gameObject);
            rootContainer = null;
        }
    }

    public void ClearAll()
    {
        foreach (KeyValuePair<TKey, WorldFeatureOwnershipGroup> pair in groups)
        {
            WorldFeatureOwnershipGroup group = pair.Value;
            group.ClearInstances();

            if (group.Root != null)
                UnityEngine.Object.Destroy(group.Root.gameObject);
        }

        groups.Clear();

        if (rootContainer != null)
        {
            UnityEngine.Object.Destroy(rootContainer.gameObject);
            rootContainer = null;
        }
    }

    private void EnsureRootContainer()
    {
        if (rootContainer != null)
            return;

        GameObject rootObject = new GameObject(rootContainerName);
        rootContainer = rootObject.transform;
    }
}