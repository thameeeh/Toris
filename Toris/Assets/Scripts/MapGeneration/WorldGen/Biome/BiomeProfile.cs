using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "WorldGen/Biome Profile", fileName = "BiomeProfile")]
public sealed class BiomeProfile : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "Plains";

    [Header("Land Shape (Mask)")]
    [Range(0f, 1f)] public float landRadius01 = 0.92f;
    public float coastlineNoiseScale = 0.0025f;
    [Range(0f, 1f)] public float coastlineNoiseStrength01 = 0.25f;

    [Header("Signals: Vegetation")]
    public float vegetationRegionScale = 0.0012f;
    [Min(0f)] public float vegetationDensity = 1.0f;

    [Header("Signals: Lakes")]
    public float lakeNoiseScale = 0.004f;
    [Range(0f, 1f)] public float lakeThreshold01 = 0.72f;

    [Header("Palette")]
    public TileBase[] groundVariants;
    public TileBase waterTile;

    [Header("Decor Layers")]
    public TileBase[] flowerDecorVariants;
    [Range(0f, 1f)] public float flowerBaseProb = 0.10f;

    public TileBase[] vegetationDecorVariants;
    [Range(0f, 1f)] public float vegetationMaxProb = 0.75f;

    #region Stamps
    [Header("Road")]
    public TileBase roadTile;
    [Range(3, 5)] public int roadWidthMin = 3;
    [Range(3, 5)] public int roadWidthMax = 5;
    public int maxRoadScanTiles = 4000;

    [Header("Site Definitions")]
    [SerializeField] private BiomeSiteDefinition[] siteDefinitions;

    [Header("Gate / Spawn")]
    [SerializeField] private GameObject gatePrefab;
    [SerializeField] public GameObject endGatePrefab;
    [SerializeField] public Vector2Int RunGateOffsetTiles = new Vector2Int(0, -1);
    public TileBase gateGroundTile;
    public int gateSize = 7;
    public GameObject GatePrefab => gatePrefab;
    public TileBase platformGroundTile;

    [Header("Wolf Dens")]
    [Min(0)] public int minWolfDenCount = 3;
    [Min(1)] public int wolfDenMinSpacingTiles = 40;
    [SerializeField] private GameObject wolfDenPrefab;
    public GameObject WolfDenPrefab => wolfDenPrefab;
    public TileBase wolfDenGroundTile;
    [Range(1, 15)] public int wolfDenStampSize = 5;
    #endregion

    public bool TryGetSiteDefinition(WorldSiteType siteType, out BiomeSiteDefinition siteDefinition)
    {
        if (siteDefinitions != null)
        {
            for (int i = 0; i < siteDefinitions.Length; i++)
            {
                BiomeSiteDefinition candidate = siteDefinitions[i];
                if (candidate.siteType != siteType)
                    continue;

                if (!candidate.IsValid)
                    continue;

                siteDefinition = candidate;
                return true;
            }
        }

        switch (siteType)
        {
            case WorldSiteType.Gate:
                if (gatePrefab != null)
                {
                    siteDefinition = new BiomeSiteDefinition(
                        WorldSiteType.Gate,
                        gatePrefab,
                        skipIfConsumed: true,
                        spawnSalt: WorldGenRunner.GateSpawnSaltValue);
                    return true;
                }
                break;

            case WorldSiteType.WolfDen:
                if (wolfDenPrefab != null)
                {
                    siteDefinition = new BiomeSiteDefinition(
                        WorldSiteType.WolfDen,
                        wolfDenPrefab,
                        skipIfConsumed: false,
                        spawnSalt: WorldGenRunner.WolfDenSpawnSaltValue);
                    return true;
                }
                break;
        }

        siteDefinition = default;
        return false;
    }
}
