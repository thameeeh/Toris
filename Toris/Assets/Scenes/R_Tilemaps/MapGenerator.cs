using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; // Added for the HashSet

public class MapGenerator : MonoBehaviour
{
    [Header("Generation Levels (0 to 1)")]
    [Range(0, 1)]
    [Tooltip("Any noise value below this will be water.")]
    public float waterLevel = 0.3f;
    [Range(0, 1)]
    [Tooltip("Any noise value below this (and above water) will be sand.")]
    public float sandLevel = 0.5f;
    [Range(0, 1)]
    [Tooltip("Any noise value below this (and above sand) will be grass.")]
    public float grassLevel = 0.8f;
    // Anything above grassLevel will be Rock

    [Header("References")]
    [Tooltip("The Tilemap to draw ground tiles onto.")]
    public Tilemap GroundTiles;
    [Tooltip("A second Tilemap (on top) for decorations like flowers.")]
    public Tilemap DecorationTiles; // --- NEW ---
    [Tooltip("The player GameObject. Must have the 'Player' tag.")]
    Transform Player;

    [Header("Tile Assets")]
    [Tooltip("Tile for grass terrain.")]
    public TileBase GrassTile;
    [Tooltip("Tile for water terrain.")]
    public TileBase WaterTile;
    [Tooltip("Tile for sand/beach terrain.")]
    public TileBase SandTile;
    [Tooltip("Tile for rock/mountain terrain.")]
    public TileBase RockTile;

    // --- NEW: Decoration Tiles ---
    [Header("Decoration Tile Assets")]
    [Tooltip("Decoration for Grass tiles (e.g., flowers).")]
    public TileBase FlowerTile;
    [Tooltip("Decoration for Sand tiles (e.g., shells).")]
    public TileBase SeashellTile;
    [Tooltip("Decoration for Rock tiles (e.g., pebbles).")]
    public TileBase PebbleTile;

    [Header("Map & Noise Settings")]
    [Tooltip("The width of the generation box around the player (in tiles).")]
    public int MapWidth = 15;
    [Tooltip("The height of the generation box around the player (in tiles).")]
    public int MapHeight = 15;
    [Tooltip("Controls the 'zoom' of the Perlin noise. Smaller values = larger continents.")]
    public float NoiseScale = 0.1f;

    // --- NEW: Decoration Noise Settings ---
    [Header("Decoration Noise Settings")]
    [Tooltip("Controls the 'zoom' of the decoration noise. Try a different value than base noise.")]
    public float DecorationNoiseScale = 0.2f;
    [Range(0, 1)]
    [Tooltip("How likely decorations are to spawn. Higher = fewer decorations.")]
    public float DecorationThreshold = 0.7f;

    private float offsetX;
    private float offsetY;
    private float decorationOffsetX; // --- NEW ---
    private float decorationOffsetY; // --- NEW ---

    // --- NEW: Variables to track previous slider values ---
    private float prevWaterLevel;
    private float prevSandLevel;
    private float prevGrassLevel;
    private int prevMapWidth;
    private int prevMapHeight;
    private float prevNoiseScale;
    // --- NEW ---
    private float prevDecorationNoiseScale;
    private float prevDecorationThreshold;

    // This will store the player's last position in grid cells
    private Vector3Int previousPlayerCellPos;
    // This will store all the positions we've already created a tile at
    private HashSet<Vector3Int> generatedTiles = new HashSet<Vector3Int>();

    void Start()
    {
        Player = GameObject.FindGameObjectWithTag("Player").transform;

        // Terrain noise offsets
        offsetX = Random.Range(0f, 999f);
        offsetY = Random.Range(0f, 999f);

        // --- NEW: Decoration noise offsets (must be different!) ---
        decorationOffsetX = Random.Range(0f, 999f);
        decorationOffsetY = Random.Range(0f, 999f);

        // --- CHANGED ---
        // Get the player's starting cell position
        previousPlayerCellPos = GroundTiles.WorldToCell(Player.position);

        // --- NEW: Store the initial values ---
        StoreCurrentSettings();

        // Generate the first set of tiles around the start position
        GenerateTilesAroundPlayer();
    }

    // --- CHANGED ---
    // We check for setting changes first, then for player movement
    void Update()
    {
        // Check if any sliders have changed
        if (SettingsHaveChanged())
        {
            // If they have, clear everything and regenerate
            RegenerateAllTiles();
            // Store the new values as the "previous" values
            StoreCurrentSettings();
        }
        // If settings haven't changed, just check for player movement
        else
        {
            Vector3Int currentPlayerCellPos = GroundTiles.WorldToCell(Player.position);
            if (currentPlayerCellPos != previousPlayerCellPos)
            {
                // Player moved! Generate new tiles at the edges
                GenerateTilesAroundPlayer();

                // Update the player's last known position
                previousPlayerCellPos = currentPlayerCellPos;
            }
        }
    }

    // --- NEW FUNCTION ---
    // Checks if any public setting has changed since the last frame
    bool SettingsHaveChanged()
    {
        return waterLevel != prevWaterLevel ||
               sandLevel != prevSandLevel ||
               grassLevel != prevGrassLevel ||
               MapWidth != prevMapWidth ||
               MapHeight != prevMapHeight ||
               NoiseScale != prevNoiseScale ||
               // --- NEW ---
               DecorationNoiseScale != prevDecorationNoiseScale ||
               DecorationThreshold != prevDecorationThreshold;
    }

    // --- NEW FUNCTION ---
    // Stores the current settings into the "prev" variables
    void StoreCurrentSettings()
    {
        prevWaterLevel = waterLevel;
        prevSandLevel = sandLevel;
        prevGrassLevel = grassLevel;
        prevMapWidth = MapWidth;
        prevMapHeight = MapHeight;
        prevNoiseScale = NoiseScale;
        // --- NEW ---
        prevDecorationNoiseScale = DecorationNoiseScale;
        prevDecorationThreshold = DecorationThreshold;
    }

    // --- NEW FUNCTION ---
    // Clears the entire map and regenerates tiles around the player
    void RegenerateAllTiles()
    {
        // Clear all tiles from the tilemap
        GroundTiles.ClearAllTiles();
        DecorationTiles.ClearAllTiles(); // --- NEW ---
        // Clear our memory of which tiles we've generated
        generatedTiles.Clear();
        // Regenerate the area around the player
        GenerateTilesAroundPlayer();
    }


    // --- RENAMED & HEAVILY CHANGED ---
    void GenerateTilesAroundPlayer()
    {
        // Get the player's current cell position
        Vector3Int playerCell = GroundTiles.WorldToCell(Player.position);

        // Calculate the bottom-left corner of the 15x15 generation box
        int halfWidth = MapWidth / 2;
        int halfHeight = MapHeight / 2;

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                // This is the actual grid cell we want to check and (maybe) draw in
                Vector3Int tilePos = new Vector3Int(playerCell.x - halfWidth + x,
                                                playerCell.y - halfHeight + y,
                                                0);

                // --- KEY CHANGE 1: Check if tile is already generated ---
                // If the set already contains this position, skip it
                if (generatedTiles.Contains(tilePos))
                {
                    continue;
                }

                // --- KEY CHANGE 2: Use the *world* position for noise ---
                // This makes the noise consistent across the entire world
                float xCoord = (float)tilePos.x * NoiseScale + offsetX;
                float yCoord = (float)tilePos.y * NoiseScale + offsetY;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);

                TileBase selectedTile;
                // --- CHANGED: Using variables instead of hard-coded numbers ---
                if (sample < waterLevel)
                {
                    selectedTile = WaterTile;
                }
                else if (sample < sandLevel)
                {
                    selectedTile = SandTile;
                }
                else if (sample < grassLevel)
                {
                    selectedTile = GrassTile;
                }
                else
                {
                    selectedTile = RockTile;
                }

                // --- KEY CHANGE 3: Set tile and *remember* it ---
                GroundTiles.SetTile(tilePos, selectedTile);
                generatedTiles.Add(tilePos);

                // --- NEW: Decoration Generation ---
                // We pass in the ground tile we just placed and its position
                GenerateDecoration(tilePos, selectedTile);
            }
        }
    }

    // --- NEW FUNCTION ---
    // Decides if a decoration should be placed on a given tile
    void GenerateDecoration(Vector3Int tilePos, TileBase groundTile)
    {
        // Don't put decorations on water
        if (groundTile == WaterTile)
        {
            return;
        }

        // Calculate the decoration noise (using different offsets/scale)
        float xCoord = (float)tilePos.x * DecorationNoiseScale + decorationOffsetX;
        float yCoord = (float)tilePos.y * DecorationNoiseScale + decorationOffsetY;
        float decorationSample = Mathf.PerlinNoise(xCoord, yCoord);

        // Only place a decoration if the noise is above our threshold
        if (decorationSample > DecorationThreshold)
        {
            TileBase decorationTile = null;

            // Pick the right decoration for the ground type
            if (groundTile == GrassTile)
            {
                decorationTile = FlowerTile;
            }
            else if (groundTile == SandTile)
            {
                decorationTile = SeashellTile;
            }
            else if (groundTile == RockTile)
            {
                decorationTile = PebbleTile;
            }

            // Place the chosen decoration on the second tilemap
            if (decorationTile != null)
            {
                DecorationTiles.SetTile(tilePos, decorationTile);
            }
        }
    }
}

