using UnityEngine;

public readonly struct ActiveSiteHandle
{
    public readonly SitePlacement Placement;
    public readonly GameObject Instance;

    public ActiveSiteHandle(SitePlacement placement, GameObject instance)
    {
        Placement = placement;
        Instance = instance;
    }
}