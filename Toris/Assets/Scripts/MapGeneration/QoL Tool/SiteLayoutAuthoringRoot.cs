using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class SiteLayoutAuthoringRoot : MonoBehaviour
{
    [Header("Authoring Root")]
    [SerializeField] private Grid authoringGrid;
    [Tooltip("This tile becomes (0,0) in the baked layout and should line up with the main site anchor, such as the grave position.")]
    [SerializeField] private Vector2Int originCell = Vector2Int.zero;
    [SerializeField] private SiteTileLayoutDefinition targetLayoutDefinition;

    [Header("Layer Tilemaps")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap decorationTilemap;
    [SerializeField] private Tilemap obstacleTilemap;
    [SerializeField] private Tilemap canopyTilemap;

    [Header("Scene Gizmo")]
    [SerializeField] private bool drawOriginGizmo = true;
    [SerializeField] private Color originGizmoColor = new Color(1f, 0.45f, 0.1f, 0.2f);

    public Grid AuthoringGrid => authoringGrid;
    public Vector2Int OriginCell => originCell;
    public SiteTileLayoutDefinition TargetLayoutDefinition => targetLayoutDefinition;
    public Tilemap GroundTilemap => groundTilemap;
    public Tilemap WaterTilemap => waterTilemap;
    public Tilemap DecorationTilemap => decorationTilemap;
    public Tilemap ObstacleTilemap => obstacleTilemap;
    public Tilemap CanopyTilemap => canopyTilemap;

    public void SetTargetLayoutDefinition(SiteTileLayoutDefinition layoutDefinition)
    {
        targetLayoutDefinition = layoutDefinition;
    }

    public void SetOriginCell(Vector2Int newOriginCell)
    {
        originCell = newOriginCell;
    }

    public bool HasAnyAssignedLayerTilemaps()
    {
        return groundTilemap != null
               || waterTilemap != null
               || decorationTilemap != null
               || obstacleTilemap != null
               || canopyTilemap != null;
    }

    public void GetAssignedTilemaps(List<Tilemap> results)
    {
        if (results == null)
            return;

        results.Clear();
        AddIfAssigned(results, groundTilemap);
        AddIfAssigned(results, waterTilemap);
        AddIfAssigned(results, decorationTilemap);
        AddIfAssigned(results, obstacleTilemap);
        AddIfAssigned(results, canopyTilemap);
    }

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawOriginGizmo)
            return;

        Grid grid = authoringGrid != null ? authoringGrid : GetComponent<Grid>();
        if (grid == null)
            return;

        Vector3Int originCellPosition = new Vector3Int(originCell.x, originCell.y, 0);
        Vector3 cornerA = GetGridPointWorld(grid, new Vector3(originCellPosition.x, originCellPosition.y, 0f));
        Vector3 cornerB = GetGridPointWorld(grid, new Vector3(originCellPosition.x + 1f, originCellPosition.y, 0f));
        Vector3 cornerC = GetGridPointWorld(grid, new Vector3(originCellPosition.x + 1f, originCellPosition.y + 1f, 0f));
        Vector3 cornerD = GetGridPointWorld(grid, new Vector3(originCellPosition.x, originCellPosition.y + 1f, 0f));
        Vector3 worldCenter = (cornerA + cornerB + cornerC + cornerD) * 0.25f;

        Color outlineColor = new Color(1f, 0.55f, 0.15f, 1f);

        Gizmos.color = outlineColor;
        Gizmos.DrawLine(cornerA, cornerB);
        Gizmos.DrawLine(cornerB, cornerC);
        Gizmos.DrawLine(cornerC, cornerD);
        Gizmos.DrawLine(cornerD, cornerA);
        Gizmos.DrawLine(cornerA, cornerC);
        Gizmos.DrawLine(cornerB, cornerD);

#if UNITY_EDITOR
        Handles.DrawSolidRectangleWithOutline(
            new[]
            {
                cornerA,
                cornerB,
                cornerC,
                cornerD
            },
            originGizmoColor,
            outlineColor);

        Vector3 labelAnchor = GetHighestCorner(cornerA, cornerB, cornerC, cornerD);
        Handles.color = new Color(1f, 0.75f, 0.3f, 1f);
        Handles.Label(
            Vector3.Lerp(worldCenter, labelAnchor, 1.15f),
            $"Origin (0,0)\nCell {originCell.x}, {originCell.y}");
#endif
    }

    private static Vector3 GetGridPointWorld(Grid grid, Vector3 cellPosition)
    {
        Vector3 localPoint = grid.CellToLocalInterpolated(cellPosition);
        return grid.LocalToWorld(localPoint);
    }

    private static Vector3 GetHighestCorner(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        Vector3 highest = a;
        if (b.y > highest.y)
            highest = b;
        if (c.y > highest.y)
            highest = c;
        if (d.y > highest.y)
            highest = d;

        return highest;
    }

    private void AutoAssignReferences()
    {
        if (authoringGrid == null)
        {
            authoringGrid = GetComponent<Grid>();
            if (authoringGrid == null)
                authoringGrid = GetComponentInChildren<Grid>(true);
        }

        Transform searchRoot = authoringGrid != null ? authoringGrid.transform : transform;

        if (groundTilemap == null)
            groundTilemap = FindTilemapByName(searchRoot, "Ground");

        if (waterTilemap == null)
            waterTilemap = FindTilemapByName(searchRoot, "Water");

        if (decorationTilemap == null)
            decorationTilemap = FindTilemapByName(searchRoot, "Decoration");

        if (obstacleTilemap == null)
            obstacleTilemap = FindTilemapByName(searchRoot, "Obstacle");

        if (canopyTilemap == null)
            canopyTilemap = FindTilemapByName(searchRoot, "Canopy");
    }

    private static void AddIfAssigned(List<Tilemap> results, Tilemap tilemap)
    {
        if (tilemap != null)
            results.Add(tilemap);
    }

    private static Tilemap FindTilemapByName(Transform root, string tilemapName)
    {
        if (root == null || string.IsNullOrEmpty(tilemapName))
            return null;

        Tilemap[] tilemaps = root.GetComponentsInChildren<Tilemap>(true);
        for (int i = 0; i < tilemaps.Length; i++)
        {
            Tilemap candidate = tilemaps[i];
            if (candidate != null && candidate.name == tilemapName)
                return candidate;
        }

        return null;
    }
}
