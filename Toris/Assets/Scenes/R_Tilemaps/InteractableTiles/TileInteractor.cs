using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // Required

public class TileInteractor : MonoBehaviour
{
    [SerializeField] private Tilemap _interactableTilemap;
    [SerializeField] private Camera _mainCamera;

    // We use Update instead of OnPointerClick for global mouse detection
    private void Update()
    {
        // 1. Check if Left Mouse Button was clicked this frame
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        Debug.Log("TileInteractor: Click detected!");

        // 2. Get Mouse Position
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(mousePos);

        worldPos.z = 0;

        // 3. Convert World Position to Grid (Cell) Position
        Vector3Int cellPos = _interactableTilemap.WorldToCell(worldPos);

        // 4. Check if a tile exists there
        TileBase clickedTile = _interactableTilemap.GetTile(cellPos);

        // 5. Check if it is our custom "ResourceTile"
        if (clickedTile is ResourceTile resourceTile)
        {
            CollectTileResource(resourceTile, cellPos);
        }
    }

    private void CollectTileResource(ResourceTile tile, Vector3Int cellPos)
    {
        Inventory.InventoryInstance.AddResource(tile.ResourceToGive, tile.ResourceAmount);

        // Remove the tile
        _interactableTilemap.SetTile(cellPos, null);

        Debug.Log($"Collected {tile.ResourceAmount} {tile.ResourceToGive.name}");
    }
}