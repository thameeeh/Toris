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
    [SerializeField] private float _groundNoiseScale = 0.07f;
    [SerializeField, Range(0f, 1f)]
    private float _rockPatchThreshold = 0.68f; // higher = fewer rock patches

    [Header("Rock Biome (Large Rock Regions)")]
    [SerializeField] private float _rockBiomeScale = 0.045f;
    [SerializeField, Range(0f, 1f)]
    private float _rockBiomeThreshold = 0.82f;

    [Header("Path Noise (Road Network)")]
    [SerializeField] private float _pathNoiseScale = 0.06f;
    [SerializeField] private float _pathVerticalScaleMultiplier = 0.5f;
    [SerializeField, Range(0f, 1f)]
    private float _pathCenter = 0.5f;
    [SerializeField, Range(0f, 0.5f)]
    private float _pathHalfWidth = 0.035f;

    [Header("Wilderness Settings")]
    [Tooltip("How far from path (in noise distance, ~0–0.5) wilderness starts.")]
    [SerializeField, Range(0f, 0.5f)]
    private float _wildernessStartDistance = 0.14f;

    // ---------------- TILE VARIANTS ----------------

    [Header("Grass Tiles (Fields)")]
    [SerializeField] private WeightedTile[] _grassTiles;

    [Header("Rock Tiles (Patches)")]
    [SerializeField] private WeightedTile[] _rockTiles;

    [Header("Road Tiles (Dirt / Rock Roads)")]
    [SerializeField] private WeightedTile[] _roadTiles;

    // ---------------- DECORATIONS ----------------

    [Header("Decoration Noise")]
    [SerializeField] private float _decorNoiseScale = 0.35f;

    [Header("Flowers on Grass")]
    [SerializeField] private WeightedTile[] _flowerDecorTiles;
    [SerializeField, Range(0f, 1f)] private float _flowerBaseDensity = 0.13f;
    [SerializeField, Range(0f, 1f)] private float _flowerWildernessBonus = 0.35f;

    [Header("Rocks on Rock Tiles")]
    [SerializeField] private WeightedTile[] _rockDecorTiles;
    [SerializeField, Range(0f, 1f)] private float _rockDecorBaseDensity = 0.07f;
    [SerializeField, Range(0f, 1f)] private float _rockDecorWildernessBonus = 0.25f;

    // ---------------- ENEMIES ----------------

    [Header("Enemy Spawning")]
    [SerializeField] private Enemy _leaderWolfPrefab; // spawns around rock decorations
    [SerializeField] private Enemy _badgerPrefab;     // spawns around flower decorations
    [SerializeField] private int _maxLeaderWolves = 4;
    [SerializeField] private int _maxBadgers = 8;

    [Range(0f, 1f)]
    [SerializeField] private float _leaderWolfChance = 0.015f;

    [Range(0f, 1f)]
    [SerializeField] private float _badgerChance = 0.04f;

    [SerializeField] private float _spawnRadius = 1.6f;

    private bool _poolReady;

    // ---------------- INTERNAL STATE ----------------

    private HashSet<Vector2Int> _generatedChunks = new HashSet<Vector2Int>();
    private Queue<Vector2Int> _chunksToGenerate = new Queue<Vector2Int>();

    // enemies per chunk (for unloading)
    private Dictionary<Vector2Int, List<Enemy>> _chunkEnemies = new Dictionary<Vector2Int, List<Enemy>>();

    // noise offsets so same seed = same world
    private Vector2 _groundOffset;
    private Vector2 _pathOffset;
    private Vector2 _decorOffset;
    private Vector2 _rockBiomeOffset;

    // ---------------- LIVE TUNING CACHE ----------------

    float _lastGroundNoiseScale;
    float _lastRockPatchThreshold;
    float _lastRockBiomeScale;
    float _lastRockBiomeThreshold;

    float _lastPathNoiseScale;
    float _lastPathVerticalScaleMultiplier;
    float _lastPathHalfWidth;

    float _lastWildernessStartDistance;

    float _lastDecorNoiseScale;
    float _lastFlowerBaseDensity;
    float _lastFlowerWildernessBonus;
    float _lastRockDecorBaseDensity;
    float _lastRockDecorWildernessBonus;

    float _lastLeaderWolfChance;
    float _lastBadgerChance;

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
        CacheCurrentSettings();
    }

    private void Update()
    {
        if (_player == null || _groundMap == null)
            return;

        EnqueueVisibleChunks();
        ProcessChunkQueue();
        UnloadFarChunks();

        // Live tuning: if any key setting changed in Inspector, rebuild world
        if (HasSettingsChanged())
        {
            RegenerateAll();
            CacheCurrentSettings();
        }
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

        _rockBiomeOffset = new Vector2(
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
        _chunkEnemies.Clear();

        // IMPORTANT: we DO NOT reseed here, so you can tweak live
        // If you want a whole new world, change _seed or toggle _useRandomSeed once.

        EnqueueVisibleChunks();
    }

    // --------------------------------------------------------------------
    // Live tuning helpers
    // --------------------------------------------------------------------

    private void CacheCurrentSettings()
    {
        _lastGroundNoiseScale = _groundNoiseScale;
        _lastRockPatchThreshold = _rockPatchThreshold;
        _lastRockBiomeScale = _rockBiomeScale;
        _lastRockBiomeThreshold = _rockBiomeThreshold;

        _lastPathNoiseScale = _pathNoiseScale;
        _lastPathVerticalScaleMultiplier = _pathVerticalScaleMultiplier;
        _lastPathHalfWidth = _pathHalfWidth;

        _lastWildernessStartDistance = _wildernessStartDistance;

        _lastDecorNoiseScale = _decorNoiseScale;
        _lastFlowerBaseDensity = _flowerBaseDensity;
        _lastFlowerWildernessBonus = _flowerWildernessBonus;
        _lastRockDecorBaseDensity = _rockDecorBaseDensity;
        _lastRockDecorWildernessBonus = _rockDecorWildernessBonus;

        _lastLeaderWolfChance = _leaderWolfChance;
        _lastBadgerChance = _badgerChance;
    }

    private bool HasSettingsChanged()
    {
        return
            !Mathf.Approximately(_groundNoiseScale, _lastGroundNoiseScale) ||
            !Mathf.Approximately(_rockPatchThreshold, _lastRockPatchThreshold) ||
            !Mathf.Approximately(_rockBiomeScale, _lastRockBiomeScale) ||
            !Mathf.Approximately(_rockBiomeThreshold, _lastRockBiomeThreshold) ||

            !Mathf.Approximately(_pathNoiseScale, _lastPathNoiseScale) ||
            !Mathf.Approximately(_pathVerticalScaleMultiplier, _lastPathVerticalScaleMultiplier) ||
            !Mathf.Approximately(_pathHalfWidth, _lastPathHalfWidth) ||

            !Mathf.Approximately(_wildernessStartDistance, _lastWildernessStartDistance) ||

            !Mathf.Approximately(_decorNoiseScale, _lastDecorNoiseScale) ||
            !Mathf.Approximately(_flowerBaseDensity, _lastFlowerBaseDensity) ||
            !Mathf.Approximately(_flowerWildernessBonus, _lastFlowerWildernessBonus) ||
            !Mathf.Approximately(_rockDecorBaseDensity, _lastRockDecorBaseDensity) ||
            !Mathf.Approximately(_rockDecorWildernessBonus, _lastRockDecorWildernessBonus) ||

            !Mathf.Approximately(_leaderWolfChance, _lastLeaderWolfChance) ||
            !Mathf.Approximately(_badgerChance, _lastBadgerChance);
    }

    // --------------------------------------------------------------------
    // Chunk helpers
    // --------------------------------------------------------------------

    private Vector2Int WorldTileToChunk(Vector3Int tilePos)
    {
        int cx = Mathf.FloorToInt((float)tilePos.x / _chunkSize);
        int cy = Mathf.FloorToInt((float)tilePos.y / _chunkSize);
        return new Vector2Int(cx, cy);
    }

    private Vector2Int WorldPosToChunk(Vector3 worldPos)
    {
        var tilePos = _groundMap.WorldToCell(worldPos);
        return WorldTileToChunk(tilePos);
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
        // 1. Despawn enemies belonging to this chunk
        if (_chunkEnemies.TryGetValue(chunk, out var enemies))
        {
            var manager = GameplayPoolManager.Instance;

            foreach (var enemy in enemies)
            {
                if (enemy == null) continue;

                if (manager != null)
                {
                    GameplayPoolManager.Instance.Release(enemy);
                }
                else
                {
                    Destroy(enemy.gameObject);
                }
            }

            enemies.Clear();
            _chunkEnemies.Remove(chunk);
        }

        // 2. Clear tiles
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

        if (TileNavWorld.Instance != null)
        {
            TileNavWorld.Instance.ClearNavChunk(chunk);
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

        // After all tiles for this chunk are placed, build nav for it
        if (TileNavWorld.Instance != null)
        {
            TileNavWorld.Instance.BuildNavChunk(chunk, _chunkSize);
        }
    }


    private void GenerateTileAt(Vector3Int pos)
    {
        // world -> noise coords for ground
        float gx = pos.x * _groundNoiseScale + _groundOffset.x;
        float gy = pos.y * _groundNoiseScale + _groundOffset.y;

        // separate noise for tile variants (micro-variation)
        float variantX = pos.x * 0.25f + 12345f;
        float variantY = pos.y * 0.25f + 54321f;
        float variantNoise = Mathf.PerlinNoise(variantX, variantY);

        // path noise
        float px = pos.x * _pathNoiseScale + _pathOffset.x;
        float py = pos.y * _pathNoiseScale * _pathVerticalScaleMultiplier + _pathOffset.y;

        float groundNoise = Mathf.PerlinNoise(gx, gy); 
        float pathNoise = Mathf.PerlinNoise(px, py);

        float distFromPath = Mathf.Abs(pathNoise - _pathCenter);

        float wildernessFactor = 0f;
        if (distFromPath > _wildernessStartDistance)
        {
            wildernessFactor = Mathf.InverseLerp(_wildernessStartDistance, 0.5f, distFromPath);
        }

        float rbx = pos.x * _rockBiomeScale + _rockBiomeOffset.x;
        float rby = pos.y * _rockBiomeScale + _rockBiomeOffset.y;
        float rockBiomeNoise = Mathf.PerlinNoise(rbx, rby);
        bool inRockBiome = rockBiomeNoise > _rockBiomeThreshold;

        bool isRoad = distFromPath < _pathHalfWidth;

        TileBase groundTile;

        if (isRoad && _roadTiles != null && _roadTiles.Length > 0)
        {
            groundTile = PickWeightedTile(_roadTiles, variantNoise);
        }
        else
        {
            if (inRockBiome)
            {
                if (_rockTiles != null && _rockTiles.Length > 0)
                {
                    float biomeStrength = Mathf.InverseLerp(_rockBiomeThreshold, 1f, rockBiomeNoise);

                    float rockChance = Mathf.Lerp(0.2f, 0.8f, biomeStrength);
                    float roll = groundNoise;

                    if (roll < rockChance && _rockTiles.Length > 0)
                        groundTile = PickWeightedTile(_rockTiles, variantNoise);
                    else
                        groundTile = PickWeightedTile(_grassTiles, variantNoise);
                }
                else
                {
                    groundTile = PickWeightedTile(_grassTiles, variantNoise);
                }
            }
            else
            {
                if (_grassTiles != null && _grassTiles.Length > 0)
                {
                    if (groundNoise > _rockPatchThreshold && _rockTiles != null && _rockTiles.Length > 0)
                    {
                        groundTile = PickWeightedTile(_rockTiles, variantNoise);
                    }
                    else
                    {
                        groundTile = PickWeightedTile(_grassTiles, variantNoise);
                    }
                }
                else
                {
                    groundTile = PickWeightedTile(_rockTiles, variantNoise);
                }
            }
        }

        _groundMap.SetTile(pos, groundTile);

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

        float dx = pos.x * _decorNoiseScale + _decorOffset.x;
        float dy = pos.y * _decorNoiseScale + _decorOffset.y;
        float decorNoise = Mathf.PerlinNoise(dx, dy);

        bool isGrass = ContainsTile(_grassTiles, groundTile);
        bool isRock = ContainsTile(_rockTiles, groundTile);

        if (isGrass && _flowerDecorTiles != null && _flowerDecorTiles.Length > 0)
        {
            float density = _flowerBaseDensity + wildernessFactor * _flowerWildernessBonus;
            if (decorNoise < density)
            {
                var flowerTile = PickWeightedTile(_flowerDecorTiles, decorNoise);
                _decorationMap.SetTile(pos, flowerTile);

                TrySpawnBadgerNearDecoration(pos);

                return;
            }
        }

        if (isRock && _rockDecorTiles != null && _rockDecorTiles.Length > 0)
        {
            float density = _rockDecorBaseDensity + wildernessFactor * _rockDecorWildernessBonus;
            if (decorNoise < density)
            {
                var rockTile = PickWeightedTile(_rockDecorTiles, decorNoise);
                _decorationMap.SetTile(pos, rockTile);

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
    // Enemy spawning (using pool, registered per chunk)
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

    private void RegisterEnemyInChunk(Enemy enemy, Vector3 worldPos)
    {
        if (enemy == null) return;

        var chunk = WorldPosToChunk(worldPos);

        if (!_chunkEnemies.TryGetValue(chunk, out var list))
        {
            list = new List<Enemy>();
            _chunkEnemies[chunk] = list;
        }

        list.Add(enemy);
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
            return;

        Vector3 center = _groundMap.GetCellCenterWorld(tilePos);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _spawnRadius;
        Vector3 spawnPos = center + (Vector3)offset;

        Enemy wolf = manager.SpawnEnemy(_leaderWolfPrefab, spawnPos, Quaternion.identity);
        if (wolf != null)
        {
            RegisterEnemyInChunk(wolf, spawnPos);
        }
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
            return;

        Vector3 center = _groundMap.GetCellCenterWorld(tilePos);
        Vector2 offset = UnityEngine.Random.insideUnitCircle * _spawnRadius;
        Vector3 spawnPos = center + (Vector3)offset;

        Enemy badger = manager.SpawnEnemy(_badgerPrefab, spawnPos, Quaternion.identity);
        if (badger != null)
        {
            RegisterEnemyInChunk(badger, spawnPos);
        }
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
