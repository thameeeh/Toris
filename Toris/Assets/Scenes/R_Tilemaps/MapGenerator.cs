using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Map References")]
    [SerializeField] private Tilemap _baseMap;        // The ground (Sand, Grass, Rock)
    [SerializeField] private Tilemap _interactibleMap; // The objects (Trees, Ores)
    [SerializeField] private Transform _player;

    [Header("Base Terrain Tiles")]
    [SerializeField] private TileBase _sandTile;      // Lowest layer
    [SerializeField] private TileBase _grassTile;     // Middle layer
    [SerializeField] private TileBase _rockFloorTile; // Highest layer

    [Header("Interactible Tiles")]
    [Tooltip("Assign your Custom ResourceTiles here")]
    [SerializeField] private TileBase _treeTile;   // Spawns on Grass
    [SerializeField] private TileBase _oreTile;    // Spawns on Rock

    [Header("Generation Settings")]
    [SerializeField] private int _mapWidth = 20;
    [SerializeField] private int _mapHeight = 20;
    [SerializeField] private float _noiseScale = 0.1f;
    [SerializeField] private float _interactibleDensity = 0.1f; // 10% chance to spawn item

    [Header("Terrain Thresholds")]
    [Range(0, 1)] public float SandLevel = 0.3f;  // Anything below this is Sand
    [Range(0, 1)] public float GrassLevel = 0.7f; // Anything between Sand and this is Grass
    // Anything above GrassLevel is Rock

    // Internal State
    private float _seedX, _seedY;
    private Vector3Int _lastPlayerPos;
    private HashSet<Vector3Int> _generatedTiles = new HashSet<Vector3Int>();

    // Tracking for Inspector changes (Hot Reload)
    private float _lastSandLevel, _lastGrassLevel;

    void Start()
    {
        if (_player == null)
            _player = GameObject.FindWithTag("Player").transform;

        _seedX = Random.Range(0f, 9999f);
        _seedY = Random.Range(0f, 9999f);

        GenerateMap();
    }

    void Update()
    {
        // 1. Check if Player moved to a new cell
        Vector3Int playerPos = _baseMap.WorldToCell(_player.position);
        if (playerPos != _lastPlayerPos)
        {
            GenerateMap();
            _lastPlayerPos = playerPos;
        }

        // 2. Check if Settings changed (Hot-Reload)
        if (SandLevel != _lastSandLevel || GrassLevel != _lastGrassLevel)
        {
            // If sliders moved, clear everything and redraw
            _baseMap.ClearAllTiles();
            _interactibleMap.ClearAllTiles();
            _generatedTiles.Clear();
            GenerateMap();

            _lastSandLevel = SandLevel;
            _lastGrassLevel = GrassLevel;
        }
    }

    void GenerateMap()
    {
        Vector3Int center = _baseMap.WorldToCell(_player.position);
        int halfW = _mapWidth / 2;
        int halfH = _mapHeight / 2;

        for (int x = -halfW; x <= halfW; x++)
        {
            for (int y = -halfH; y <= halfH; y++)
            {
                Vector3Int pos = new Vector3Int(center.x + x, center.y + y, 0);

                if (_generatedTiles.Contains(pos)) continue;

                GenerateTileAt(pos);
                _generatedTiles.Add(pos);
            }
        }
    }

    void GenerateTileAt(Vector3Int pos)
    {
        // 1. Calculate Perlin Noise (0.0 to 1.0)
        float xCoord = (pos.x * _noiseScale) + _seedX;
        float yCoord = (pos.y * _noiseScale) + _seedY;
        float noise = Mathf.PerlinNoise(xCoord, yCoord);

        TileBase groundToPlace = null;
        TileBase interactibleToPlace = null;

        // 2. Determine Terrain Type
        if (noise < SandLevel)
        {
            // Bottom Layer: Sand
            groundToPlace = _sandTile;
            // Usually no interactibles on sand, but you can add cacti here if you want
        }
        else if (noise < GrassLevel)
        {
            // Middle Layer: Grass
            groundToPlace = _grassTile;

            // Check for Trees
            if (ShouldSpawnInteractible(xCoord, yCoord))
            {
                interactibleToPlace = _treeTile;
            }
        }
        else
        {
            // Top Layer: Rock Floor
            groundToPlace = _rockFloorTile;

            // Check for Ores
            if (ShouldSpawnInteractible(xCoord, yCoord))
            {
                interactibleToPlace = _oreTile;
            }
        }

        // 3. Set the tiles
        _baseMap.SetTile(pos, groundToPlace);

        if (interactibleToPlace != null)
        {
            _interactibleMap.SetTile(pos, interactibleToPlace);
        }
    }

    // Helper function to calculate random chance for items
    private bool ShouldSpawnInteractible(float x, float y)
    {
        // We multiply coords by 5 to make the "item noise" more random/scattered 
        // compared to the smooth terrain noise.
        float itemNoise = Mathf.PerlinNoise(x * 5f, y * 5f);
        return itemNoise > (1f - _interactibleDensity);
    }
}