using UnityEngine;
using UnityEngine.Tilemaps;

public class NavSafeSpawn : MonoBehaviour
{
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private int searchRadius = 10; // in tiles

    private void Start()
    {
        SnapToNearestWalkable();
    }

    private void SnapToNearestWalkable()
    {
        if (TileNavWorld.Instance == null)
        {
            Debug.LogWarning("[NavSafeSpawn] No TileNavWorld in scene.");
            return;
        }

        if (groundMap == null)
        {
            Debug.LogWarning("[NavSafeSpawn] No ground Tilemap assigned.");
            return;
        }

        Vector3 startPos = transform.position;

        // If already on walkable ground (not water), do nothing
        if (TileNavWorld.Instance.IsWalkableWorldPos(startPos))
            return;

        Vector3Int baseCell = groundMap.WorldToCell(startPos);

        for (int r = 1; r <= searchRadius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    var cell = new Vector3Int(baseCell.x + dx, baseCell.y + dy, 0);
                    Vector3 worldPos = groundMap.GetCellCenterWorld(cell);

                    if (TileNavWorld.Instance.IsWalkableWorldPos(worldPos))
                    {
                        transform.position = worldPos;
                        return;
                    }
                }
            }
        }

        //Debug.LogWarning("[NavSafeSpawn] Could not find walkable spawn near " + startPos);
    }
}
