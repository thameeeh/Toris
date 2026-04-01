using System.Collections.Generic;
using UnityEngine;

public sealed class PersistentWorldFeatureLifecycle
{
    private readonly WorldContext worldContext;
    private readonly WorldPoiPoolManager poiPoolManager;
    private readonly WorldSiteActivationPipeline worldSiteActivationPipeline;

    private const string PersistentOwnershipKey = "persistent";
    private readonly WorldFeatureOwnershipCollection<string> persistentOwnership;
    private SitePlacementIndex sitePlacementIndex;

    public PersistentWorldFeatureLifecycle(
        WorldContext worldContext,
        WorldPoiPoolManager poiPoolManager,
        WorldSiteActivationPipeline worldSiteActivationPipeline)
    {
        this.worldContext = worldContext;
        this.poiPoolManager = poiPoolManager;
        this.worldSiteActivationPipeline = worldSiteActivationPipeline;

        persistentOwnership = new WorldFeatureOwnershipCollection<string>(
            poiPoolManager,
            "PersistentWorldFeatures_Root",
            _ => "PersistentWorldFeatures");
    }

    public void RebuildPlacements()
    {
        sitePlacementIndex = worldContext != null && worldContext.BuildOutput != null
            ? worldContext.BuildOutput.SitePlacements
            : null;
    }

    public void ClearAll()
    {
        sitePlacementIndex = null;
        persistentOwnership.ClearAll();
    }

    public void ActivatePersistentSites()
    {
        if (sitePlacementIndex == null || worldContext == null)
            return;

        IReadOnlyList<SitePlacement> persistentPlacements = sitePlacementIndex.PersistentBiomePlacements;
        if (persistentPlacements == null || persistentPlacements.Count == 0)
            return;

        WorldFeatureOwnershipGroup ownershipGroup =
            persistentOwnership.GetOrCreateGroup(PersistentOwnershipKey);

        if (ownershipGroup.InstanceCount > 0)
            return;

        for (int i = 0; i < persistentPlacements.Count; i++)
        {
            SitePlacement placement = persistentPlacements[i];
            if (placement.SiteDefinition == null || !placement.SiteDefinition.IsValid)
                continue;

            GameObject siteObject = worldSiteActivationPipeline != null
                ? worldSiteActivationPipeline.ActivateSite(
                    placement,
                    ownershipGroup.Root,
                    worldContext.ActiveBiome.Seed)
                : null;

            if (siteObject != null)
                ownershipGroup.AddInstance(siteObject);
        }
    }

    public int GetActiveSiteCount()
    {
        if (!persistentOwnership.TryGetGroup(PersistentOwnershipKey, out WorldFeatureOwnershipGroup ownershipGroup))
            return 0;

        return ownershipGroup.InstanceCount;
    }
}
