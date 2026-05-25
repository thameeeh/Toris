using UnityEngine;
using UnityEngine.Tilemaps;

[DefaultExecutionOrder(-200)]
public sealed class PrologueNavigationBootstrap : MonoBehaviour
{
    [Header("Navigation Tilemaps")]
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private Tilemap waterMap;
    [SerializeField] private Tilemap obstacleMap;

    [Header("Build")]
    [SerializeField, Min(1)] private int chunkSize = 32;
    [SerializeField, Min(0)] private int boundsPaddingCells = 4;

    private void Awake()
    {
        BuildNavigation();
    }

    public void BuildNavigation()
    {
        if (groundMap == null)
        {
#if UNITY_EDITOR
            Debug.LogError($"[{nameof(PrologueNavigationBootstrap)}] Ground tilemap is missing.", this);
#endif
            return;
        }

        TileNavWorld navWorld = ResolveNavigationWorld();
        navWorld.Initialize(groundMap, waterMap, obstacleMap);
        navWorld.SetNavigationContributions(null);

        if (!TryResolveBuildBounds(out BoundsInt buildBounds))
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[{nameof(PrologueNavigationBootstrap)}] No tilemap bounds found to build navigation.", this);
#endif
            return;
        }

        int minChunkX = Mathf.FloorToInt((buildBounds.xMin - boundsPaddingCells) / (float)chunkSize);
        int maxChunkX = Mathf.FloorToInt((buildBounds.xMax - 1 + boundsPaddingCells) / (float)chunkSize);
        int minChunkY = Mathf.FloorToInt((buildBounds.yMin - boundsPaddingCells) / (float)chunkSize);
        int maxChunkY = Mathf.FloorToInt((buildBounds.yMax - 1 + boundsPaddingCells) / (float)chunkSize);

        int builtChunkCount = 0;
        for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
        {
            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                navWorld.BuildNavChunk(new Vector2Int(chunkX, chunkY), chunkSize);
                builtChunkCount++;
            }
        }

#if UNITY_EDITOR
        Debug.Log($"[{nameof(PrologueNavigationBootstrap)}] Built {builtChunkCount} navigation chunks for Prologue.", this);
#endif
    }

    private TileNavWorld ResolveNavigationWorld()
    {
        TileNavWorld navWorld = TileNavWorld.Instance;
        if (navWorld != null)
            return navWorld;

        GameObject navObject = new GameObject("TileNavWorld");
        return navObject.AddComponent<TileNavWorld>();
    }

    private bool TryResolveBuildBounds(out BoundsInt buildBounds)
    {
        buildBounds = default;
        bool hasBounds = false;

        hasBounds = TryExpandBounds(groundMap, ref buildBounds, hasBounds);
        hasBounds = TryExpandBounds(waterMap, ref buildBounds, hasBounds);
        hasBounds = TryExpandBounds(obstacleMap, ref buildBounds, hasBounds);

        return hasBounds;
    }

    private static bool TryExpandBounds(Tilemap tilemap, ref BoundsInt buildBounds, bool hasBounds)
    {
        if (tilemap == null || tilemap.GetUsedTilesCount() <= 0)
            return hasBounds;

        BoundsInt tilemapBounds = tilemap.cellBounds;
        if (!hasBounds)
        {
            buildBounds = tilemapBounds;
            return true;
        }

        int xMin = Mathf.Min(buildBounds.xMin, tilemapBounds.xMin);
        int yMin = Mathf.Min(buildBounds.yMin, tilemapBounds.yMin);
        int xMax = Mathf.Max(buildBounds.xMax, tilemapBounds.xMax);
        int yMax = Mathf.Max(buildBounds.yMax, tilemapBounds.yMax);

        buildBounds.SetMinMax(
            new Vector3Int(xMin, yMin, 0),
            new Vector3Int(xMax, yMax, 1));

        return true;
    }

#if UNITY_EDITOR
    private void Reset()
    {
        AutoAssignTilemaps();
    }

    private void OnValidate()
    {
        chunkSize = Mathf.Max(1, chunkSize);
        boundsPaddingCells = Mathf.Max(0, boundsPaddingCells);

        if (groundMap == null || waterMap == null || obstacleMap == null)
            AutoAssignTilemaps();
    }

    private void AutoAssignTilemaps()
    {
        Tilemap[] childTilemaps = GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < childTilemaps.Length; i++)
        {
            Tilemap tilemap = childTilemaps[i];
            if (tilemap == null)
                continue;

            if (tilemap.name == "Terrain")
                groundMap = tilemap;
            else if (tilemap.name == "Water")
                waterMap = tilemap;
            else if (tilemap.name == "Obstacle")
                obstacleMap = tilemap;
        }
    }
#endif
}
