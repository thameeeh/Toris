using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.InputSystem; // Required

public class TileInteractor : MonoBehaviour
{
    [SerializeField] private Tilemap _interactableTilemap;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private InputActionReference interactAction;


    ResourceTile closestResource = null;
    Vector3Int targetCell = Vector3Int.zero;
    private void OnEnable()
    {
        interactAction.action.Enable();
        interactAction.action.performed += HandleInteract;
    }

    private void OnDisable()
    {
        interactAction.action.performed -= HandleInteract;
        interactAction.action.Disable();
    }

    // We use Update instead of OnPointerClick for global mouse detection
    private void Update()
    {
        // 1. Check if Left Mouse Button was clicked this frame
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            //HandleClick();
        }
        Debug.Log("Player: Interact detected!");

        // 1. Get Player Position (World & Cell)
        Vector3 playerPos = transform.position;
        Vector3Int playerCell = _interactableTilemap.WorldToCell(playerPos);

        // 2. Define search range (e.g., 1 tile in every direction)
        int range = 5;

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
            Debug.Log($"Found closest resource at: {targetCell}");
            CollectTileResource(closestResource, targetCell);
        }
        else
        {
            Debug.Log("No interactable tile nearby.");
        }
        closestResource = null;
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        

       
    }

    private void CollectTileResource(ResourceTile tile, Vector3Int cellPos)
    {
        Inventory.InventoryInstance.AddResource(tile.ResourceToGive, tile.ResourceAmount);

        // Remove the tile
        _interactableTilemap.SetTile(cellPos, null);

        //Debug.Log($"Collected {tile.ResourceAmount} {tile.ResourceToGive.name}");
    }
}