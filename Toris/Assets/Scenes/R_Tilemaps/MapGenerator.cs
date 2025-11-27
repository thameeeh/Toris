using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[Serializable]
public class WeightedTile
{
    public TileBase tile;
    [Range(0f, 1f)] public float weight = 1f;
}

public class MapGenerator : MonoBehaviour
{
    [Header("Tilemaps")]
    [SerializeField] private Tilemap _groundMap;      // Grass / Rock / Road
    [SerializeField] private Tilemap _decorationMap;  // Flowers / small rocks on top

    [Header("Player / Focus")]
    [SerializeField] private Transform _player;

    // ---------------- SEED & CHUNK SETTINGS ----------------

    [Header("Seed Settings")]
    [SerializeField] private bool _useRandomSeed = true;
    [SerializeField] private int _seed = 12345;

    [Header("Chunk Settings")]
    [SerializeField] private int _chunkSize = 32;
    [SerializeField] private int _viewDistanceInChunks = 2;
    [SerializeField] private int _maxChunksPerFrame = 1;
    [SerializeField] private int _unloadBuffer = 1; // how many chunks beyond view to keep

    // ---------------- NOISE SETTINGS ----------------

    [Header("Ground Noise (Fields / Patches)")]
    [SerializeField] private float _groundNoiseScale = 0.05f;
    [SerializeField, Range(0f, 1f)]
    private float _rockPatchThreshold = 0.7f; // higher = fewer rock patches

    [Header("Path Noise (Road Network)")]
    [SerializeField] private float _pathNoiseScale = 0.04f;
    [SerializeField] private float _pathVerticalScaleMultiplier = 0.5f;
    [SerializeField, Range(0f, 1f)]
    private float _pathCenter = 0.5f;
    [SerializeField, Range(0f, 0.5f)]
    private float _pathHalfWidth = 0.04f;

    [Header("Wilderness Settings")]
    [Tooltip("How far from path (in noise distance, 0–0.5) wilderness starts.")]
    [SerializeField, Range(0f, 0.5f)]
    private float _wildernessStartDistance = 0.12f;

    // ---------------- TILE VARIANTS ----------------

    [Header("Grass Tiles (Fields)")]
    [SerializeField] private WeightedTile[] _grassTiles;

    [Header("Rock Tiles (Patches)")]
    [SerializeField] private WeightedTile[] _rockTiles;

    [Header("Road Tiles (Dirt / Rock Roads)")]
    [SerializeField] private WeightedTile[] _roadTiles;

    // ---------------- DECORATIONS ----------------

    [Header("Decoration Noise")]
    [SerializeField] private float _decorNoiseScale = 0.3f;

    [Header("Flowers on Grass")]
    [SerializeField] private WeightedTile[] _flowerDecorTiles;
    [SerializeField, Range(0f, 1f)] private float _flowerBaseDensity = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _flowerWildernessBonus = 0.25f;

    [Header("Rocks on Rock Tiles")]
    [SerializeField] private WeightedTile[] _rockDecorTiles;
    [SerializeField, Range(0f, 1f)] private float _rockDecorBaseDensity = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _rockDecorWildernessBonus = 0.25f;

    // ---------------- ENEMIES ----------------

    [Header("Enemy Spawning")]
    [SerializeField] private Enemy _leaderWolfPrefab; // spawns around rock decorations
    [SerializeField] private Enemy _badgerPrefab;     // spawns around flower decorations
    [SerializeField] private int _maxLeaderWolves = 4;
    [SerializeField] private int _maxBadgers = 8;

    // Chance a rock decoration will spawn a leader wolf nearby
    [Range(0f, 1f)]
    [SerializeField] private float _leaderWolfChance = 0.02f;

    // Chance a flower decoration will spawn a badger nearby
    [Range(0f, 1f)]
    [SerializeField] private float _badgerChance = 0.05f;

    // How far from the tile center to drop the enemy
    [SerializeField] private float _spawnRadius = 1.5f;

    private bool _poolReady;

    // ---------------- INTERNAL STATE ----------------

    private HashSet<Vector2Int> _generatedChunks = new HashSet<Vector2Int>();
    private Queue<Vector2Int> _chunksToGenerate = new Queue<Vector2Int>();

    // noise offsets so same seed = same world
    private Vector2 _groundOffset;
    private Vector2 _pathOffset;
    private Vector2 _decorOffset;

    // --------------------------------------------------------------------
    // Unity lifecycle
    // --------------------------------------------------------------------

    private void Awake()
    {
        InitializeSeed();
    }

    private void Start()
    {
        if (_player == null)
            _player = GameObject.FindWithTag("Player")?.transform;

        EnqueueVisibleChunks();
    }

    private void Update()
    {
        if (_player == null || _groundMap == null)
            return;

        EnqueueVisibleChunks();
        ProcessChunkQueue();
        UnloadFarChunks();
    }

    // --------------------------------------------------------------------
    // Seed / noise init
    // --------------------------------------------------------------------

    private void InitializeSeed()
    {
        if (_useRandomSeed)
        {
            _seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        }

        var rng = new System.Random(_seed);

        _groundOffset = new Vector2(
            rng.Next(-100000, 100000),
            rng.Next(-100000, 100000));

        _pathOffset = new Vector2(
            rng.Next(-100000, 100000),
            rng.Next(-100000, 100000));

        _decorOffset = new Vector2(
            rng.Next(-100000, 100000),
            rng.Next(-100000, 100000));
    }

    [ContextMenu("Regenerate All")]
    private void RegenerateAll()
    {
        _groundMap.ClearAllTiles();
        if (_decorationMap != null)
            _decorationMap.ClearAllTiles();

        _generatedChunks.Clear();
        _chunksToGenerate.Clear();

        InitializeSeed();
        EnqueueVisibleChunks();
    }

    // --------------------------------------------------------------------
    // Chunk management
    // --------------------------------------------------------------------

    private Vector2Int WorldTileToChunk(Vector3Int tilePos)
    {
        int cx = Mathf.FloorToInt((float)tilePos.x / _chunkSize);
        int cy = Mathf.FloorToInt((float)tilePos.y / _chunkSize);
        return new Vector2Int(cx, cy);
    }

    private void EnqueueVisibleChunks()
    {
        if (_player == null) return;

        var playerTile = _groundMap.WorldToCell(_player.position);
        var centerChunk = WorldTileToChunk(playerTile);

        for (int dx = -_viewDistanceInChunks; dx <= _viewDistanceInChunks; dx++)
        {
            for (int dy = -_viewDistanceInChunks; dy <= _viewDistanceInChunks; dy++)
            {
                var chunk = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);
                if (_generatedChunks.Contains(chunk))
                    continue;

                if (!_chunksToGenerate.Contains(chunk))
                    _chunksToGenerate.Enqueue(chunk);
            }
        }
    }

    private void ProcessChunkQueue()
    {
        int generatedThisFrame = 0;

        while (_chunksToGenerate.Count > 0 && generatedThisFrame < _maxChunksPerFrame)
        {
            var chunk = _chunksToGenerate.Dequeue();
            GenerateChunk(chunk);
            _generatedChunks.Add(chunk);
            generatedThisFrame++;
        }
    }

    private void UnloadFarChunks()
    {
        if (_player == null) return;

        var playerTile = _groundMap.WorldToCell(_player.position);
        var centerChunk = WorldTileToChunk(playerTile);

        var toRemove = new List<Vector2Int>();

        foreach (var chunk in _generatedChunks)
        {
            int distX = Mathf.Abs(chunk.x - centerChunk.x);
            int distY = Mathf.Abs(chunk.y - centerChunk.y);
            int maxDist = Mathf.Max(distX, distY);

            if (maxDist > _viewDistanceInChunks + _unloadBuffer)
            {
                ClearChunk(chunk);
                toRemove.Add(chunk);
            }
        }

        foreach (var c in toRemove)
            _generatedChunks.Remove(c);
    }

    private void ClearChunk(Vector2Int chunk)
    {
        int startX = chunk.x * _chunkSize;
        int startY = chunk.y * _chunkSize;

        for (int lx = 0; lx < _chunkSize; lx++)
        {
            for (int ly = 0; ly < _chunkSize; ly++)
            {
                var tilePos = new Vector3Int(startX + lx, startY + ly, 0);
                _groundMap.SetTile(tilePos, null);
                if (_decorationMap != null)
                    _decorationMap.SetTile(tilePos, null);
            }
        }
    }

    // --------------------------------------------------------------------
    // Chunk generation
    // --------------------------------------------------------------------

    private void GenerateChunk(Vector2Int chunk)
    {
        int startX = chunk.x * _chunkSize;
        int startY = chunk.y * _chunkSize;

        for (int lx = 0; lx < _chunkSize; lx++)
        {
            for (int ly = 0; ly < _chunkSize; ly++)
            {
                var tilePos = new Vector3Int(startX + lx, startY + ly, 0);
                GenerateTileAt(tilePos);
            }
        }
    }

    private void GenerateTileAt(Vector3Int pos)
    {
        // world -> noise coords
        float gx = pos.x * _groundNoiseScale + _groundOffset.x;
        float gy = pos.y * _groundNoiseScale + _groundOffset.y;

        float px = pos.x * _pathNoiseScale + _pathOffset.x;
        float py = pos.y * _pathNoiseScale * _pathVerticalScaleMultiplier + _pathOffset.y;

        // base noises
        float groundNoise = Mathf.PerlinNoise(gx, gy); // patches / variation
        float pathNoise = Mathf.PerlinNoise(px, py);   // roads

        // distance from road center line in noise space
        float distFromPath = Mathf.Abs(pathNoise - _pathCenter);

        // wilderness factor: 0 near roads, 1 far from roads
        float wildernessFactor = 0f;
        if (distFromPath > _wildernessStartDistance)
        {
            // remap [startDistance .. 0.5] -> [0 .. 1]
            wildernessFactor = Mathf.InverseLerp(_wildernessStartDistance, 0.5f, distFromPath);
        }

        // ---- base ground: grass vs rock patches ----
        bool isRockPatch = groundNoise > _rockPatchThreshold;
        bool isRoad = distFromPath < _pathHalfWidth;

        TileBase groundTile;

        if (isRoad && _roadTiles != null && _roadTiles.Length > 0)
        {
            groundTile = PickWeightedTile(_roadTiles, groundNoise);
        }
        else if (isRockPatch && _rockTiles != null && _rockTiles.Length > 0)
        {
            groundTile = PickWeightedTile(_rockTiles, groundNoise);
        }
        else
        {
            groundTile = PickWeightedTile(_grassTiles, groundNoise);
        }

        _groundMap.SetTile(pos, groundTile);

        // ---- decorations on top (and enemy spawn hooks) ----
        if (_decorationMap != null)
        {
            SpawnDecoration(pos, groundTile, wildernessFactor);
        }
    }

    // --------------------------------------------------------------------
    // Tile / decoration helpers
    // --------------------------------------------------------------------

    private TileBase PickWeightedTile(WeightedTile[] tiles, float sample)
    {
        if (tiles == null || tiles.Length == 0)
            return null;

        float totalWeight = 0f;
        foreach (var wt in tiles)
        {
            if (wt == null || wt.tile == null) continue;
            totalWeight += Mathf.Max(0f, wt.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float v = (sample % 1f) * totalWeight;

        foreach (var wt in tiles)
        {
            if (wt == null || wt.tile == null) continue;
            float w = Mathf.Max(0f, wt.weight);
            if (v <= w)
                return wt.tile;

            v -= w;
        }

        // fallback
        for (int i = tiles.Length - 1; i >= 0; i--)
        {
            if (tiles[i] != null && tiles[i].tile != null)
                return tiles[i].tile;
        }

        return null;
    }

    private void SpawnDecoration(Vector3Int pos, TileBase groundTile, float wildernessFactor)
    {
        if (groundTile == null) return;

        // decor noise
        float dx = pos.x * _decorNoiseScale + _decorOffset.x;
        float dy = pos.y * _decorNoiseScale + _decorOffset.y;
        float decorNoise = Mathf.PerlinNoise(dx, dy);

        // simple helpers
        bool isGrass = ContainsTile(_grassTiles, groundTile);
        bool isRock = ContainsTile(_rockTiles, groundTile);

        // FLOWERS ON GRASS (+ badger spawn)
        if (isGrass && _flowerDecorTiles != null && _flowerDecorTiles.Length > 0)
        {
            float density = _flowerBaseDensity + wildernessFactor * _flowerWildernessBonus;
            if (decorNoise < density)
            {
                var flowerTile = PickWeightedTile(_flowerDecorTiles, decorNoise);
                _decorationMap.SetTile(pos, flowerTile);

                // badger spawn around flower decorations
                TrySpawnBadgerNearDecoration(pos);

                return; // only one decor per tile
            }
        }

        // ROCKS ON ROCK TILES (+ wolf spawn)
        if (isRock && _rockDecorTiles != null && _rockDecorTiles.Length > 0)
        {
            float density = _rockDecorBaseDensity + wildernessFactor * _rockDecorWildernessBonus;
            if (decorNoise < density)
            {
                var rockTile = PickWeightedTile(_rockDecorTiles, decorNoise);
                _decorationMap.SetTile(pos, rockTile);

                // wolf spawn around rock decorations
                TrySpawnLeaderWolfNearDecoration(pos);

                return;
            }
        }
    }

    private bool ContainsTile(WeightedTile[] tiles, TileBase tile)
    {
        if (tiles == null || tile == null) return false;
        foreach (var wt in tiles)
        {
            if (wt != null && wt.tile == tile)
                return true;
        }
        return false;
    }

    // --------------------------------------------------------------------
    // Enemy spawning (using pool)
    // --------------------------------------------------------------------

    private void TryResolvePool()
    {
        if (_poolReady) return;

        if (GameplayPoolManager.Instance != null)
        {
            _poolReady = true;
            return;
        }

        var found = FindFirstObjectByType<GameplayPoolManager>();
        if (found != null)
        {
            _poolReady = true;
        }
    }

    private void TrySpawnLeaderWolfNearDecoration(Vector3Int tilePos)
    {
        if (_leaderWolfPrefab == null) return;
        if (UnityEngine.Random.value > _leaderWolfChance) return;

        TryResolvePool();
        if (!_poolReady) return;

        var manager = GameplayPoolManager.Instance;
        if (manager == null) return;

        var report = manager.GetEnemyReport(_leaderWolfPrefab);
        if (report.Active >= _maxLeaderWolves)
        {
            return;
        }

        Vector3 center = _groundMap.GetCellCenterWorld(tilePos);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _spawnRadius;
        Vector3 spawnPos = center + (Vector3)offset;

        manager.SpawnEnemy(_leaderWolfPrefab, spawnPos, Quaternion.identity);
    }

    private void TrySpawnBadgerNearDecoration(Vector3Int tilePos)
    {
        if (_badgerPrefab == null) return;
        if (UnityEngine.Random.value > _badgerChance) return;

        TryResolvePool();
        if (!_poolReady) return;

        var manager = GameplayPoolManager.Instance;
        if (manager == null) return;

        var report = manager.GetEnemyReport(_badgerPrefab);
        if (report.Active >= _maxBadgers)
        {
            return;
        }

        Vector3 center = _groundMap.GetCellCenterWorld(tilePos);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _spawnRadius;
        Vector3 spawnPos = center + (Vector3)offset;

        manager.SpawnEnemy(_badgerPrefab, spawnPos, Quaternion.identity);
    }

    // --------------------------------------------------------------------
    // Gizmos (optional – shows chunk grid in editor)
    // --------------------------------------------------------------------

    private void OnDrawGizmosSelected()
    {
        if (_player == null || _groundMap == null) return;

        var playerTile = _groundMap.WorldToCell(_player.position);
        var centerChunk = WorldTileToChunk(playerTile);

        Gizmos.color = Color.yellow;

        for (int dx = -_viewDistanceInChunks; dx <= _viewDistanceInChunks; dx++)
        {
            for (int dy = -_viewDistanceInChunks; dy <= _viewDistanceInChunks; dy++)
            {
                var chunk = new Vector2Int(centerChunk.x + dx, centerChunk.y + dy);
                int startX = chunk.x * _chunkSize;
                int startY = chunk.y * _chunkSize;

                Vector3 worldMin = _groundMap.CellToWorld(new Vector3Int(startX, startY, 0));
                Vector3 worldMax = _groundMap.CellToWorld(new Vector3Int(startX + _chunkSize, startY + _chunkSize, 0));

                Vector3 size = worldMax - worldMin;
                Vector3 center = worldMin + size * 0.5f;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}
