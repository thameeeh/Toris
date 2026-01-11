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
    //[Range(0f, 1f)] public float vegetationStart01 = 0.30f;
    //[Range(0f, 1f)] public float vegetationFull01 = 0.45f;
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

    [Header("Stamps")]
    public TileBase roadTile;
    [Range(3, 5)] public int roadWidthMin = 3;
    [Range(3, 5)] public int roadWidthMax = 5;

    [SerializeField] private GameObject gatePrefab;
    public GameObject GatePrefab => gatePrefab;

    public TileBase platformGroundTile;
    public TileBase gateGroundTile;
    public int gateSize = 7;
    public int maxRoadScanTiles = 4000;
}
