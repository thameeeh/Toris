using UnityEngine;
using UnityEngine.Tilemaps;

// Right-click in Project > Create > Tiles > ResourceTile to create one
[CreateAssetMenu(fileName = "New Resource Tile", menuName = "Tiles/ResourceTile")]
public class ResourceTile : Tile
{
    [Header("Resource Data")]
    public ResourceData ResourceToGive;
    public int ResourceAmount = 1;

    // Optional: Tooltip or specific logic usually goes here, 
    // but for simple data storage, this is all you need.
}