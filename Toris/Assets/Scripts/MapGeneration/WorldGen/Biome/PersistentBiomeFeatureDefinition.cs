using System;
using UnityEngine;

[Serializable]
public struct PersistentBiomeFeatureDefinition
{
    [SerializeField] private WorldSiteDefinition siteDefinition;
    [SerializeField] private Vector2Int tileOffsetFromBiomeOrigin;

    public WorldSiteDefinition SiteDefinition => siteDefinition;
    public Vector2Int TileOffsetFromBiomeOrigin => tileOffsetFromBiomeOrigin;

    public bool IsValid => siteDefinition != null && siteDefinition.IsValid;
}