using UnityEngine;

public sealed class WorldGenDebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGenRunner runner;
    [SerializeField] private Grid grid;
    [SerializeField] private Transform followTarget;

    [Header("UI")]
    [SerializeField] private bool visible = true;
    [SerializeField] private Vector2 panelPos = new Vector2(12, 12);
    [SerializeField] private Vector2 panelSize = new Vector2(320, 150);
    [SerializeField] private int fontSize = 14;

    private readonly TileResolver resolver = new TileResolver();
    private GUIStyle style;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
            visible = !visible;
    }

    private void OnGUI()
    {
        if (!visible) return;

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                richText = false,
                wordWrap = false
            };
        }
        else if (style.fontSize != fontSize)
        {
            style.fontSize = fontSize;
        }

        if (runner == null)
        {
            GUI.Box(new Rect(panelPos.x, panelPos.y, panelSize.x, 60), "WorldGen Debug");
            GUI.Label(new Rect(panelPos.x + 10, panelPos.y + 25, panelSize.x - 20, 20), "runner == null", style);
            return;
        }

        WorldContext ctx = runner.Context;
        if (ctx == null)
        {
            GUI.Box(new Rect(panelPos.x, panelPos.y, panelSize.x, 60), "WorldGen Debug");
            GUI.Label(new Rect(panelPos.x + 10, panelPos.y + 25, panelSize.x - 20, 20), "runner.Context == null", style);
            return;
        }

        Vector2 focusWorld = followTarget != null ? (Vector2)followTarget.position : Vector2.zero;

        Vector2Int focusTile;
        if (grid != null)
        {
            Vector3Int cell = grid.WorldToCell((Vector3)focusWorld);
            focusTile = new Vector2Int(cell.x, cell.y);
        }
        else
        {
            focusTile = new Vector2Int(Mathf.FloorToInt(focusWorld.x), Mathf.FloorToInt(focusWorld.y));
        }

        WorldSignals s = resolver.sampler.Compute(focusTile, ctx);

        string biomeName = ctx.Biome != null ? ctx.Biome.displayName : "(null biome)";

        Rect r = new Rect(panelPos.x, panelPos.y, panelSize.x, panelSize.y);
        GUI.Box(r, "WorldGen Debug");

        GUILayout.BeginArea(new Rect(r.x + 10, r.y + 24, r.width - 20, r.height - 34));
        GUILayout.Label($"Biome: {biomeName} (idx {ctx.ActiveBiome.Index})", style);
        GUILayout.Label($"Tile: {focusTile}", style);
        GUILayout.Label($"dist01: {s.dist01:F2}", style);
        GUILayout.Label($"danger01: {s.danger01:F2}", style);
        GUILayout.Label($"veg01: {s.vegetation01:F2}", style);
        GUILayout.Label($"lake01: {s.lake01:F2}", style);
        GUILayout.EndArea();
    }
}
