using System;
using UnityEngine;

[Serializable]
public struct BiomeSiteDefinition
{
    public WorldSiteType siteType;
    public GameObject prefab;
    public bool skipIfConsumed;
    public uint spawnSalt;

    public BiomeSiteDefinition(
        WorldSiteType siteType,
        GameObject prefab,
        bool skipIfConsumed,
        uint spawnSalt)
    {
        this.siteType = siteType;
        this.prefab = prefab;
        this.skipIfConsumed = skipIfConsumed;
        this.spawnSalt = spawnSalt;
    }

    public bool IsValid => prefab != null;
}