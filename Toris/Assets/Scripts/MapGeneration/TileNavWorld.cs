using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileNavWorld : MonoBehaviour
{
    public static TileNavWorld Instance { get; private set; }

    [Header("Tilemap used for navigation")]
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private Tilemap waterMap;

    [Header("Walkable tiles (whitelist)")]
    [SerializeField] private TileBase[] walkableTiles;

    [Header("Blocking tiles (override)")]
    [SerializeField] private TileBase[] blockingTiles;

    // One nav grid per chunk
    private readonly Dictionary<Vector2Int, NavChunk> _navChunks = new();

    private HashSet<TileBase> _walkableSet;
    private HashSet<TileBase> _blockingSet;

    // We assume one global chunk size for nav; set from first BuildNavChunk call
    private int _chunkSize;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (!groundMap)
            Debug.LogError("[TileNavWorld] groundMap not assigned!");

        _walkableSet = new HashSet<TileBase>(walkableTiles ?? new TileBase[0]);
        _blockingSet = new HashSet<TileBase>(blockingTiles ?? new TileBase[0]);
    }

    // --- Public API: used by MapGenerator ---

    public void BuildNavChunk(Vector2Int chunkCoord, int chunkSize)
    {
        if (!groundMap) return;

        // Initialize global chunk size the first time we see one
        if (_chunkSize == 0)
        {
            _chunkSize = chunkSize;
        }
        else if (_chunkSize != chunkSize)
        {
            Debug.LogWarning($"[TileNavWorld] BuildNavChunk called with chunkSize {chunkSize}, " +
                             $"but existing size is {_chunkSize}. Using {_chunkSize} for nav lookups.");
        }

        var navChunk = new NavChunk(chunkCoord, chunkSize);

        int startX = chunkCoord.x * chunkSize;
        int startY = chunkCoord.y * chunkSize;

        for (int lx = 0; lx < chunkSize; lx++)
        {
            for (int ly = 0; ly < chunkSize; ly++)
            {
                var cell = new Vector3Int(startX + lx, startY + ly, 0);

                TileBase tile = groundMap.GetTile(cell);
                bool walkable = IsTileWalkable(tile);
                
                if (waterMap != null)
                {
                    TileBase waterTile = waterMap.GetTile(cell);
                    if (waterTile != null)
                    {
                        walkable = false;
                    }
                }
                
                navChunk.SetWalkable(lx, ly, walkable);
            }
        }

        _navChunks[chunkCoord] = navChunk;
    }

    public void ClearNavChunk(Vector2Int chunkCoord)
    {
        _navChunks.Remove(chunkCoord);
    }

    // --- Public API: used by pathfinding / AI ---

    /// <summary>
    /// Convert world position to tile coordinates.
    /// </summary>
    public Vector2Int WorldToCell(Vector3 worldPos)
    {
        if (!groundMap) return Vector2Int.zero;
        Vector3Int cell = groundMap.WorldToCell(worldPos);
        return new Vector2Int(cell.x, cell.y);
    }

    /// <summary>
    /// Convert tile coordinates to world position (center of tile).
    /// </summary>
    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        if (!groundMap)
            return new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0);

        return groundMap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    /// <summary>
    /// Is the given world cell (tile coordinate) walkable according to nav data?
    /// </summary>
    public bool IsWalkableCell(Vector2Int worldCell)
    {
        int chunkSize = _chunkSize;
        // Use floor division to match MapGenerator.WorldTileToChunk
        int cx = Mathf.FloorToInt(worldCell.x / (float)chunkSize);
        int cy = Mathf.FloorToInt(worldCell.y / (float)chunkSize);
        var chunkCoord = new Vector2Int(cx, cy);

        if (!_navChunks.TryGetValue(chunkCoord, out var navChunk))
            return false; // no data => treat as non-walkable

        int localX = worldCell.x - (chunkCoord.x * chunkSize);
        int localY = worldCell.y - (chunkCoord.y * chunkSize);

        if (localX < 0 || localY < 0 || localX >= chunkSize || localY >= chunkSize)
            return false;

        return navChunk.GetWalkable(localX, localY);
    }


    /// <summary>
    /// Quick helper for worldPosition -> tile cell -> walkable.
    /// </summary>
    public bool IsWalkableWorldPos(Vector3 worldPos)
    {
        if (!groundMap) return false;
        Vector3Int cell = groundMap.WorldToCell(worldPos);
        return IsWalkableCell(new Vector2Int(cell.x, cell.y));
    }

    // --- Tile rules (independent of MapGenerator) ---

    private bool IsTileWalkable(TileBase tile)
    {
        if (tile == null) return false;

        if (_blockingSet.Contains(tile))
            return false;

        if (_walkableSet.Contains(tile))
            return true;

        // default: not walkable
        return false;
    }

    // --- Internal NavChunk type ---

    private readonly struct NavChunk
    {
        public readonly Vector2Int Coord;
        public readonly int Size;
        private readonly bool[,] _walkable; // [localX, localY]

        public NavChunk(Vector2Int coord, int size)
        {
            Coord = coord;
            Size = size;
            _walkable = new bool[size, size];
        }

        public void SetWalkable(int x, int y, bool value)
        {
            _walkable[x, y] = value;
        }

        public bool GetWalkable(int x, int y)
        {
            return _walkable[x, y];
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        if (_chunkSize <= 0) return;

        // Draw walkable / blocked tiles for all loaded nav chunks
        Gizmos.matrix = Matrix4x4.identity;

        foreach (var kvp in _navChunks)
        {
            var chunk = kvp.Value;
            int size = chunk.Size;
            Vector2Int chunkCoord = chunk.Coord;

            int startX = chunkCoord.x * size;
            int startY = chunkCoord.y * size;

            for (int lx = 0; lx < size; lx++)
            {
                for (int ly = 0; ly < size; ly++)
                {
                    bool walkable = chunk.GetWalkable(lx, ly);

                    Vector2Int cell = new Vector2Int(startX + lx, startY + ly);
                    Vector3 worldCenter = CellToWorldCenter(cell);

                    Gizmos.color = walkable ? new Color(0f, 1f, 0f, 0.25f) : new Color(1f, 0f, 0f, 0.25f);
                    Gizmos.DrawCube(worldCenter, Vector3.one * 0.9f);
                }
            }
        }
    }
#endif

}
