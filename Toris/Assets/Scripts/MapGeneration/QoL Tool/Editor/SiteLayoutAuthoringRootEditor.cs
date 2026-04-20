using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(SiteLayoutAuthoringRoot))]
public sealed class SiteLayoutAuthoringRootEditor : Editor
{
    private delegate void AssignTileDelegate(ref SiteTileLayoutCell cell, TileBase tile);

    private readonly List<Tilemap> _tilemapBuffer = new();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SiteLayoutAuthoringRoot root = (SiteLayoutAuthoringRoot)target;
        bool canBake = TryBuildBakeCells(root, out _, out string bakeMessage);

        EditorGUILayout.Space(12f);
        EditorGUILayout.HelpBox(
            "Select this authoring root in the Scene view to see the orange Origin (0,0) marker. " +
            "That marked tile is the site anchor tile, such as where the grave sits.",
            MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reset Origin To (0,0)", GUILayout.Height(24f)))
            {
                ResetOrigin(root);
            }

            if (GUILayout.Button("Frame Origin In Scene", GUILayout.Height(24f)))
            {
                FrameOriginInScene(root);
            }
        }

        EditorGUILayout.Space(8f);
        DrawSummary(root);

        EditorGUILayout.Space(8f);

        if (GUILayout.Button("Validate Layout", GUILayout.Height(28f)))
        {
            ValidateLayout(root, logSuccess: true);
        }

        using (new EditorGUI.DisabledScope(!canBake))
        {
            if (GUILayout.Button("Bake New Layout", GUILayout.Height(28f)))
            {
                BakeNewLayout(root);
            }
        }

        using (new EditorGUI.DisabledScope(root.TargetLayoutDefinition == null || !canBake))
        {
            if (GUILayout.Button("Rebake Existing Layout", GUILayout.Height(28f)))
            {
                RebakeExistingLayout(root);
            }
        }

        if (!canBake)
        {
            EditorGUILayout.HelpBox(bakeMessage, MessageType.Warning);
        }
    }

    private static void ResetOrigin(SiteLayoutAuthoringRoot root)
    {
        if (root == null)
            return;

        Undo.RecordObject(root, "Reset Site Layout Origin");
        root.SetOriginCell(Vector2Int.zero);
        EditorUtility.SetDirty(root);
        SceneView.RepaintAll();
    }

    private static void FrameOriginInScene(SiteLayoutAuthoringRoot root)
    {
        if (root == null || root.AuthoringGrid == null)
        {
            Debug.LogWarning("[SiteLayoutAuthoringRoot] Cannot frame origin because the authoring Grid is missing.", root);
            return;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("[SiteLayoutAuthoringRoot] No active Scene view is available to frame the origin.", root);
            return;
        }

        Vector3Int originCell = new Vector3Int(root.OriginCell.x, root.OriginCell.y, 0);
        Vector3 worldCenter = root.AuthoringGrid.GetCellCenterWorld(originCell);
        Vector3 cellSize = root.AuthoringGrid.cellSize;
        float frameSize = Mathf.Max(
            1.5f,
            Mathf.Max(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y)) * 4f);

        sceneView.LookAt(worldCenter, sceneView.rotation, frameSize);
        sceneView.Repaint();
        SceneView.RepaintAll();
    }

    private void DrawSummary(SiteLayoutAuthoringRoot root)
    {
        if (root == null)
            return;

        if (!root.HasAnyAssignedLayerTilemaps())
        {
            EditorGUILayout.HelpBox(
                "Assign the authoring Grid and at least one layer Tilemap before baking.",
                MessageType.Info);
            return;
        }

        if (!TryBuildBakeCells(root, out List<SiteTileLayoutCell> cells, out string message))
        {
            EditorGUILayout.HelpBox(message, MessageType.Warning);
            return;
        }

        BoundsInt paintedBounds = CalculatePaintedBounds(root);
        EditorGUILayout.HelpBox(
            $"Painted cells: {cells.Count}\n" +
            $"Origin cell: {root.OriginCell}\n" +
            $"Painted bounds: min ({paintedBounds.xMin}, {paintedBounds.yMin}) max ({paintedBounds.xMax - 1}, {paintedBounds.yMax - 1})",
            MessageType.None);
    }

    private void ValidateLayout(SiteLayoutAuthoringRoot root, bool logSuccess)
    {
        if (!TryBuildBakeCells(root, out List<SiteTileLayoutCell> cells, out string message))
        {
            Debug.LogWarning($"[SiteLayoutAuthoringRoot] Validation failed: {message}", root);
            return;
        }

        if (!logSuccess)
            return;

        Debug.Log(
            $"[SiteLayoutAuthoringRoot] Layout validation passed for '{root.name}'. " +
            $"Painted cells={cells.Count}, origin={root.OriginCell}.",
            root);
    }

    private void BakeNewLayout(SiteLayoutAuthoringRoot root)
    {
        if (!TryBuildBakeCells(root, out List<SiteTileLayoutCell> cells, out string message))
        {
            Debug.LogWarning($"[SiteLayoutAuthoringRoot] Cannot bake layout: {message}", root);
            return;
        }

        string suggestedName = $"{root.name}_Layout";
        string assetPath = EditorUtility.SaveFilePanelInProject(
            "Create Site Tile Layout Definition",
            suggestedName,
            "asset",
            "Choose where to save the baked SiteTileLayoutDefinition asset.");

        if (string.IsNullOrEmpty(assetPath))
            return;

        SiteTileLayoutDefinition layoutAsset = CreateInstance<SiteTileLayoutDefinition>();
        layoutAsset.ReplaceCells(cells);

        AssetDatabase.CreateAsset(layoutAsset, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Undo.RecordObject(root, "Assign Site Tile Layout Definition");
        root.SetTargetLayoutDefinition(layoutAsset);
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(layoutAsset);

        Debug.Log(
            $"[SiteLayoutAuthoringRoot] Created new SiteTileLayoutDefinition at '{assetPath}' with {cells.Count} painted cells.",
            layoutAsset);
    }

    private void RebakeExistingLayout(SiteLayoutAuthoringRoot root)
    {
        if (root.TargetLayoutDefinition == null)
        {
            Debug.LogWarning("[SiteLayoutAuthoringRoot] No target layout definition is assigned to rebake.", root);
            return;
        }

        if (!TryBuildBakeCells(root, out List<SiteTileLayoutCell> cells, out string message))
        {
            Debug.LogWarning($"[SiteLayoutAuthoringRoot] Cannot rebake layout: {message}", root);
            return;
        }

        Undo.RecordObject(root.TargetLayoutDefinition, "Rebake Site Tile Layout Definition");
        root.TargetLayoutDefinition.ReplaceCells(cells);
        EditorUtility.SetDirty(root.TargetLayoutDefinition);
        AssetDatabase.SaveAssets();

        Debug.Log(
            $"[SiteLayoutAuthoringRoot] Rebaked '{root.TargetLayoutDefinition.name}' with {cells.Count} painted cells.",
            root.TargetLayoutDefinition);
    }

    private bool TryBuildBakeCells(
        SiteLayoutAuthoringRoot root,
        out List<SiteTileLayoutCell> bakedCells,
        out string validationMessage)
    {
        bakedCells = null;

        if (root == null)
        {
            validationMessage = "Authoring root is missing.";
            return false;
        }

        if (root.AuthoringGrid == null)
        {
            validationMessage = "Authoring Grid is not assigned.";
            return false;
        }

        root.GetAssignedTilemaps(_tilemapBuffer);
        if (_tilemapBuffer.Count == 0)
        {
            validationMessage = "No layer Tilemaps are assigned.";
            return false;
        }

        Dictionary<Vector2Int, SiteTileLayoutCell> cellMap = new();

        BakeLayer(root.GroundTilemap, root.OriginCell, cellMap, AssignGroundTile);
        BakeLayer(root.WaterTilemap, root.OriginCell, cellMap, AssignWaterTile);
        BakeLayer(root.DecorationTilemap, root.OriginCell, cellMap, AssignDecorationTile);
        BakeLayer(root.ObstacleTilemap, root.OriginCell, cellMap, AssignObstacleTile);
        BakeLayer(root.CanopyTilemap, root.OriginCell, cellMap, AssignCanopyTile);

        if (cellMap.Count == 0)
        {
            validationMessage = "No painted tiles were found on the assigned Tilemaps.";
            return false;
        }

        bakedCells = new List<SiteTileLayoutCell>(cellMap.Values);
        bakedCells.Sort(CompareCells);

        validationMessage = null;
        return true;
    }

    private static BoundsInt CalculatePaintedBounds(SiteLayoutAuthoringRoot root)
    {
        BoundsInt? bounds = null;

        ExpandBounds(root.GroundTilemap, ref bounds);
        ExpandBounds(root.WaterTilemap, ref bounds);
        ExpandBounds(root.DecorationTilemap, ref bounds);
        ExpandBounds(root.ObstacleTilemap, ref bounds);
        ExpandBounds(root.CanopyTilemap, ref bounds);

        return bounds ?? new BoundsInt(Vector3Int.zero, Vector3Int.one);
    }

    private static void ExpandBounds(Tilemap tilemap, ref BoundsInt? bounds)
    {
        if (tilemap == null)
            return;

        BoundsInt layerBounds = tilemap.cellBounds;
        bool foundTile = false;
        Vector3Int min = Vector3Int.zero;
        Vector3Int maxExclusive = Vector3Int.zero;

        foreach (Vector3Int position in layerBounds.allPositionsWithin)
        {
            if (tilemap.GetTile(position) == null)
                continue;

            if (!foundTile)
            {
                min = position;
                maxExclusive = position + Vector3Int.one;
                foundTile = true;
                continue;
            }

            min = Vector3Int.Min(min, position);
            maxExclusive = Vector3Int.Max(maxExclusive, position + Vector3Int.one);
        }

        if (!foundTile)
            return;

        if (!bounds.HasValue)
        {
            bounds = new BoundsInt(min, maxExclusive - min);
            return;
        }

        BoundsInt current = bounds.Value;
        Vector3Int mergedMin = Vector3Int.Min(current.min, min);
        Vector3Int mergedMax = Vector3Int.Max(current.max, maxExclusive);
        bounds = new BoundsInt(mergedMin, mergedMax - mergedMin);
    }

    private static void BakeLayer(
        Tilemap tilemap,
        Vector2Int originCell,
        Dictionary<Vector2Int, SiteTileLayoutCell> cellMap,
        AssignTileDelegate assignTile)
    {
        if (tilemap == null || cellMap == null || assignTile == null)
            return;

        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int cellPosition in bounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cellPosition);
            if (tile == null)
                continue;

            Vector2Int offset = new Vector2Int(cellPosition.x - originCell.x, cellPosition.y - originCell.y);

            if (!cellMap.TryGetValue(offset, out SiteTileLayoutCell cell))
            {
                cell = new SiteTileLayoutCell
                {
                    offset = offset
                };
            }

            assignTile(ref cell, tile);
            cellMap[offset] = cell;
        }
    }

    private static void AssignGroundTile(ref SiteTileLayoutCell cell, TileBase tile) => cell.ground = tile;
    private static void AssignWaterTile(ref SiteTileLayoutCell cell, TileBase tile) => cell.water = tile;
    private static void AssignDecorationTile(ref SiteTileLayoutCell cell, TileBase tile) => cell.decoration = tile;
    private static void AssignObstacleTile(ref SiteTileLayoutCell cell, TileBase tile) => cell.obstacle = tile;
    private static void AssignCanopyTile(ref SiteTileLayoutCell cell, TileBase tile) => cell.canopy = tile;

    private static int CompareCells(SiteTileLayoutCell a, SiteTileLayoutCell b)
    {
        int yCompare = a.offset.y.CompareTo(b.offset.y);
        if (yCompare != 0)
            return yCompare;

        return a.offset.x.CompareTo(b.offset.x);
    }
}
