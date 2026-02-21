using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "WorldGen/World Gen Profile", fileName = "WorldGenProfile")]
public sealed class WorldGenProfile : ScriptableObject
{
    [Header("World")]
    public int seed = 12345;
    public float worldRadiusTiles = 1500f;
    public Vector2 spawnPosTiles = Vector2.zero;

    [Header("Chunking")]
    public int chunkSize = 32;
    public int viewDistanceChunks = 2;

    [Header("Progression")]
    public AnimationCurve dangerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Island")]
    [Range(0f, 1f)] public float islandRadius01 = 0.92f;
    public float coastNoiseScale = 0.0025f;
    [Range(0f, 1f)] public float coastNoiseStrength01 = 0.25f;

    [Header("Forest Overlay (Biome 2)")]
    [Range(0f, 1f)] public float forestStart01 = 0.30f;
    [Range(0f, 1f)] public float forestFull01 = 0.45f;
    public float forestRegionScale = 0.0012f;
    public float forestDensityMultiplier = 1.0f;

    [Header("Lakes")]
    public float lakeScale = 0.004f;
    [Range(0f, 1f)] public float lakeThreshold01 = 0.72f;

    [Header("Roads (v1 main roads)")]
    public bool enableRoads = true;
    public int mainRoadCount = 4;
    public int roadControlPointCount = 14;
    public float roadMaxTurnDeg = 25f;
    public float roadWidthFalloffTiles = 250f;
    public Vector2 roadHalfWidthNearFar = new Vector2(2.5f, 1.4f);
    public Vector2 roadSegmentLenMinMax = new Vector2(20f, 250f);

    [Header("Tile Assets")]
    public TileBase[] plainsGroundVariants;
    public TileBase waterTile;
    public TileBase[] flowerDecorVariants;
    public TileBase[] treeDecorVariants;
    public TileBase roadTile;

    [Header("Stamps")]
    public TileBase platformGroundTile;
    public TileBase gateGroundTile;

    [Range(3, 5)] public int roadWidthMin = 3;
    [Range(3, 5)] public int roadWidthMax = 5;

    public int gateSize = 7;
    public int maxRoadScanTiles = 4000;
}
