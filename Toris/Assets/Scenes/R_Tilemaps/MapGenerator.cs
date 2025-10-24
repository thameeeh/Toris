using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    public Tilemap GroundTiles;

    public TileBase GrassTile;
    public TileBase WaterTile;
    public TileBase SandTile;
    public TileBase RockTile;


    public int MapWidth = 100;
    public int MapHeight = 100;

    public float NoiseScale = 0.1f;
    private float offsetX;
    private float offsetY;

    void Start()
    {
        offsetX = Random.Range(0f, 9999f);
        offsetY = Random.Range(0f, 9999f);

        GenerateMap();
    }

    void GenerateMap() 
    {
        for(int x = 0; x < MapWidth; x++) 
        {
            for(int y = 0; y < MapHeight; y++) 
            {
                float xCoord = x * NoiseScale + offsetX;
                float yCoord = y * NoiseScale + offsetY;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                TileBase selectedTile;
                if(sample < 0.3f) 
                {
                    selectedTile = WaterTile;
                } 
                else if(sample < 0.5f) 
                {
                    selectedTile = SandTile;
                } 
                else if(sample < 0.8f) 
                {
                    selectedTile = GrassTile;
                } 
                else 
                {
                    selectedTile = RockTile;
                }
                GroundTiles.SetTile(new Vector3Int(x, y, 0), selectedTile);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
