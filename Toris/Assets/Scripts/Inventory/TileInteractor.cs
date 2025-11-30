using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // Required

public class TileInteractor : MonoBehaviour
{
    [SerializeField] private Tilemap _interactableTilemap;

    [SerializeField] private int PickRange;

    ResourceTile closestResource = null;
    Vector3Int targetCell = Vector3Int.zero;

    public void HandleInteract()
    {
        // 1. Get Player Position (World & Cell)
        Vector3 playerPos = transform.position;
        Vector3Int playerCell = _interactableTilemap.WorldToCell(playerPos);

        // 2. Define search range (e.g., 1 tile in every direction)
        int range = PickRange;

        float closestDistSqr = float.MaxValue;

        // 3. Iterate through neighbors (square loop around player)
        for (int x = -range; x <= range; x++)
        {
            for (int y = -range; y <= range; y++)
            {
                // Calculate neighbor position
                Vector3Int checkPos = new Vector3Int(playerCell.x + x, playerCell.y + y, playerCell.z);

                // Skip if there is no tile here to save calculation time
                if (!_interactableTilemap.HasTile(checkPos)) continue;

                TileBase tile = _interactableTilemap.GetTile(checkPos);

                // Check if it's the right type
                if (tile is ResourceTile resourceTile)
                {
                    // Calculate distance from Player (World Space) to Tile Center
                    Vector3 tileWorldCenter = _interactableTilemap.GetCellCenterWorld(checkPos);

                    // Use SqrMagnitude for performance (avoids square roots)
                    float distSqr = (playerPos - tileWorldCenter).sqrMagnitude;

                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        closestResource = resourceTile;
                        targetCell = checkPos;
                    }
                }
            }
        }
        // 4. Action
        if (closestResource != null)
        {
            CollectTileResource(closestResource, targetCell);
        }
        closestResource = null;
    }

    private void CollectTileResource(ResourceTile tile, Vector3Int cellPos)
    {

        if (tile.ResourceToGive == null || tile.ResourceAmount <= 0)
        {
            Debug.LogWarning("Tile has no resource to give!");
            return;
        }

        Inventory.InventoryInstance.AddResource(tile.ResourceToGive, tile.ResourceAmount);

        // Remove the tile
        _interactableTilemap.SetTile(cellPos, null);

        //Debug.Log($"Collected {tile.ResourceAmount} {tile.ResourceToGive.name}");
    }
}