using UnityEngine;
using UnityEngine.Tilemaps;

public sealed class WorldGenDebugHUD : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WorldGenProfile profile;
    [SerializeField] private Grid grid;
    [SerializeField] private bool show = true;

    private WorldContext ctx;
    private WorldSignalSampler sampler;
    private TileResolver resolver;

    private void Awake()
    {
        if (profile == null) return;

        ctx = new WorldContext(profile);
        sampler = new WorldSignalSampler();
        resolver = new TileResolver();
    }

    private void OnGUI()
    {
        if (!show || profile == null) return;

        Vector2Int tile = GetMouseTile();
        WorldSignals s = sampler.Compute(tile, ctx);
        TileResult r = resolver.Resolve(tile, ctx);

        float panelWidth = 260f;
        float panelHeight = 220f;

        Rect rect = new Rect(
            Screen.width - panelWidth - 10,
            10,
            panelWidth,
            panelHeight
        );

        GUI.Box(rect, "WorldGen Debug");

        GUILayout.BeginArea(rect);
        GUILayout.Space(20);

        GUILayout.Label($"Tile: {tile.x}, {tile.y}");
        GUILayout.Space(5);

        GUILayout.Label($"dist01: {s.dist01:F3}");
        GUILayout.Label($"danger01: {s.danger01:F3}");
        //GUILayout.Label($"islandMask: {s.islandMask01:F3}");
        GUILayout.Label($"forest01: {s.forest01:F3}");
        GUILayout.Label($"lake01: {s.lake01:F3}");
        GUILayout.Label($"road01: {s.road01:F3}");

        GUILayout.Space(5);
        GUILayout.Label($"Result:");
        GUILayout.Label($"  Water: {(r.HasWater ? "YES" : "no")}");
        GUILayout.Label($"  Ground: {(r.ground != null ? r.ground.name : "none")}");
        GUILayout.Label($"  Decor: {(r.decor != null ? r.decor.name : "none")}");

        GUILayout.EndArea();
    }

    private Vector2Int GetMouseTile()
    {
        Camera cam = Camera.main;
        if (cam == null) return Vector2Int.zero;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell(mouseWorld);
            return new Vector2Int(cell.x, cell.y);
        }

        return new Vector2Int(
            Mathf.FloorToInt(mouseWorld.x),
            Mathf.FloorToInt(mouseWorld.y)
        );
    }
}
