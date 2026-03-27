using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class WorldGenDebugHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldGenRunner runner;
    [SerializeField] private Grid grid;
    [SerializeField] private Transform followTarget;

    [Header("UI")]
    [SerializeField] private bool visible = true;
    [SerializeField] private Vector2 panelPos = new Vector2(12, 12);
    [SerializeField] private Vector2 panelSize = new Vector2(360, 210);
    [SerializeField] private int fontSize = 14;

    [Header("Gameplay Debug Visuals")]
    [SerializeField] private bool drawChunkBorders = true;
    [SerializeField] private bool drawStreamingRects = false;
    [SerializeField] private float zOffset = -0.1f;

    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    private readonly TileResolver resolver = new TileResolver();
    private GUIStyle style;

    private bool TryGetDiagnosticsSnapshot(out WorldGenDiagnosticsSnapshot diagnosticsSnapshot)
    {
        if (runner == null)
        {
            diagnosticsSnapshot = default;
            return false;
        }

        diagnosticsSnapshot = runner.CreateDiagnosticsSnapshot();
        return true;
    }
    // ---------- runtime line rendering ----------
    private Material lineMat;

    private void EnsureLineMaterial()
    {
        if (lineMat != null) return;

        Shader shader = Shader.Find("Hidden/Internal-Colored");
        if (shader == null) return;

        lineMat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        lineMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        lineMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        lineMat.SetInt("_Cull", (int)CullMode.Off);
        lineMat.SetInt("_ZWrite", 0);
    }

    private bool IsSRPActive => GraphicsSettings.currentRenderPipeline != null;

    private void OnEnable()
    {
        if (IsSRPActive)
            RenderPipelineManager.endCameraRendering += OnEndCameraRenderingSRP;
    }

    private void OnDisable()
    {
        if (IsSRPActive)
            RenderPipelineManager.endCameraRendering -= OnEndCameraRenderingSRP;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;

            // by equaling to visible force all to match
            // do new debug shows here
            drawChunkBorders = visible;
            drawStreamingRects = visible;
        }
    }

    private void OnGUI()
    {
        if (!visible)
            return;

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
            GUI.Box(new Rect(panelPos.x, panelPos.y, panelSize.x, 60f), "WorldGen Debug");
            GUI.Label(
                new Rect(panelPos.x + 10f, panelPos.y + 25f, panelSize.x - 20f, 20f),
                "runner == null",
                style);
            return;
        }

        WorldContext worldContext = runner.Context;
        if (worldContext == null)
        {
            GUI.Box(new Rect(panelPos.x, panelPos.y, panelSize.x, 60f), "WorldGen Debug");
            GUI.Label(
                new Rect(panelPos.x + 10f, panelPos.y + 25f, panelSize.x - 20f, 20f),
                "runner.Context == null",
                style);
            return;
        }

        if (!TryGetDiagnosticsSnapshot(out WorldGenDiagnosticsSnapshot diagnosticsSnapshot))
        {
            GUI.Box(new Rect(panelPos.x, panelPos.y, panelSize.x, 60f), "WorldGen Debug");
            GUI.Label(
                new Rect(panelPos.x + 10f, panelPos.y + 25f, panelSize.x - 20f, 20f),
                "diagnostics unavailable",
                style);
            return;
        }

        Vector2 focusWorldPosition = followTarget != null ? (Vector2)followTarget.position : Vector2.zero;

        Vector2Int focusTile;
        if (grid != null)
        {
            Vector3Int focusCell = grid.WorldToCell((Vector3)focusWorldPosition);
            focusTile = new Vector2Int(focusCell.x, focusCell.y);
        }
        else
        {
            focusTile = new Vector2Int(
                Mathf.FloorToInt(focusWorldPosition.x),
                Mathf.FloorToInt(focusWorldPosition.y));
        }

        WorldSignals worldSignals = resolver.sampler.Compute(focusTile, worldContext);
        string biomeName = worldContext.Biome != null ? worldContext.Biome.displayName : "(null biome)";

        Rect panelRect = new Rect(panelPos.x, panelPos.y, panelSize.x, panelSize.y);
        GUI.Box(panelRect, $"WorldGen Debug ({toggleKey})");

        GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 24f, panelRect.width - 20f, panelRect.height - 34f));

        GUILayout.Label("Visuals:", style);
        drawChunkBorders = GUILayout.Toggle(drawChunkBorders, " Chunk borders");
        drawStreamingRects = GUILayout.Toggle(drawStreamingRects, " Streaming rects (load/unload)");
        GUILayout.Space(6f);

        GUILayout.Label($"Biome: {biomeName} (idx {worldContext.ActiveBiome.Index})", style);
        GUILayout.Label($"Tile: {focusTile}", style);
        GUILayout.Label($"dist01: {worldSignals.dist01:F2}", style);
        GUILayout.Label($"danger01: {worldSignals.danger01:F2}", style);
        GUILayout.Label($"veg01: {worldSignals.vegetation01:F2}", style);
        GUILayout.Label($"lake01: {worldSignals.lake01:F2}", style);
        GUILayout.Space(6f);

        GUILayout.Label($"Loaded chunks: {diagnosticsSnapshot.LoadedChunkCount}", style);
        GUILayout.Label($"Active site chunks: {diagnosticsSnapshot.ActiveSiteChunkCount}", style);
        GUILayout.Label($"Active sites: {diagnosticsSnapshot.ActiveSiteCount}", style);
        GUILayout.Label($"Placed sites: {diagnosticsSnapshot.TotalPlacedSiteCount}", style);
        GUILayout.Label($"Preload chunks: {diagnosticsSnapshot.PreloadChunks}", style);
        GUILayout.Label($"Unload hysteresis: {diagnosticsSnapshot.UnloadHysteresisChunks}", style);

        GUILayout.EndArea();
    }
    private void OnRenderObject()
    {
        if (IsSRPActive) return;
        DrawDebugLinesForCamera(Camera.current);
    }

    private void OnEndCameraRenderingSRP(ScriptableRenderContext context, Camera cam)
    {
        DrawDebugLinesForCamera(cam);
    }

    private void DrawDebugLinesForCamera(Camera cam)
    {
        if (!visible)
            return;

        if (!drawChunkBorders && !drawStreamingRects)
            return;

        if (runner == null || runner.Context == null)
            return;

        if (grid == null)
            return;

        if (cam == null)
            return;

        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)
            return;

        if (!TryGetDiagnosticsSnapshot(out WorldGenDiagnosticsSnapshot diagnosticsSnapshot))
            return;

        WorldProfile worldProfile = diagnosticsSnapshot.Profile;
        if (worldProfile == null)
            return;

        EnsureLineMaterial();
        if (lineMat == null)
            return;

        lineMat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);

        int chunkSize = worldProfile.chunkSize;

        if (drawChunkBorders)
        {
            IReadOnlyCollection<Vector2Int> loadedChunks = diagnosticsSnapshot.LoadedChunks;
            if (loadedChunks != null)
            {
                GL.Color(new Color(0f, 1f, 0f, 0.75f));
                foreach (Vector2Int chunkCoord in loadedChunks)
                {
                    DrawChunkRectGL(chunkCoord, chunkSize);
                }
            }
        }

        if (drawStreamingRects)
        {
            Camera streamCamera = diagnosticsSnapshot.StreamCamera;
            if (streamCamera != null)
            {
                GetCameraChunkRect(streamCamera, worldProfile, out Vector2Int loadMinChunk, out Vector2Int loadMaxChunk);

                int padding = Mathf.Max(0, worldProfile.viewDistanceChunks) + Mathf.Max(0, diagnosticsSnapshot.PreloadChunks);
                loadMinChunk -= new Vector2Int(padding, padding);
                loadMaxChunk += new Vector2Int(padding, padding);

                int unloadHysteresis = Mathf.Max(0, diagnosticsSnapshot.UnloadHysteresisChunks);
                Vector2Int unloadMinChunk = loadMinChunk - new Vector2Int(unloadHysteresis, unloadHysteresis);
                Vector2Int unloadMaxChunk = loadMaxChunk + new Vector2Int(unloadHysteresis, unloadHysteresis);

                GL.Color(new Color(0f, 0.6f, 1f, 0.9f));
                DrawChunkRangeRectGL(loadMinChunk, loadMaxChunk, chunkSize);

                GL.Color(new Color(1f, 0.6f, 0f, 0.9f));
                DrawChunkRangeRectGL(unloadMinChunk, unloadMaxChunk, chunkSize);
            }
        }

        GL.End();
        GL.PopMatrix();
    }
    private void DrawChunkRectGL(Vector2Int chunk, int chunkSize)
    {
        int baseX = chunk.x * chunkSize;
        int baseY = chunk.y * chunkSize;

        Vector3 p0 = grid.CellToWorld(new Vector3Int(baseX, baseY, 0)); p0.z = zOffset;
        Vector3 p1 = grid.CellToWorld(new Vector3Int(baseX + chunkSize, baseY, 0)); p1.z = zOffset;
        Vector3 p2 = grid.CellToWorld(new Vector3Int(baseX + chunkSize, baseY + chunkSize, 0)); p2.z = zOffset;
        Vector3 p3 = grid.CellToWorld(new Vector3Int(baseX, baseY + chunkSize, 0)); p3.z = zOffset;

        Line(p0, p1);
        Line(p1, p2);
        Line(p2, p3);
        Line(p3, p0);
    }

    private void DrawChunkRangeRectGL(Vector2Int minChunk, Vector2Int maxChunk, int chunkSize)
    {
        int minX = minChunk.x * chunkSize;
        int minY = minChunk.y * chunkSize;
        int maxX = (maxChunk.x + 1) * chunkSize;
        int maxY = (maxChunk.y + 1) * chunkSize;

        Vector3 p0 = grid.CellToWorld(new Vector3Int(minX, minY, 0)); p0.z = zOffset;
        Vector3 p1 = grid.CellToWorld(new Vector3Int(maxX, minY, 0)); p1.z = zOffset;
        Vector3 p2 = grid.CellToWorld(new Vector3Int(maxX, maxY, 0)); p2.z = zOffset;
        Vector3 p3 = grid.CellToWorld(new Vector3Int(minX, maxY, 0)); p3.z = zOffset;

        Line(p0, p1);
        Line(p1, p2);
        Line(p2, p3);
        Line(p3, p0);
    }

    private static void Line(Vector3 a, Vector3 b)
    {
        GL.Vertex(a);
        GL.Vertex(b);
    }

    private void GetCameraChunkRect(Camera cam, WorldProfile prof, out Vector2Int minChunk, out Vector2Int maxChunk)
    {
        float zPlane = 0f;
        float dist = DistanceAlongCameraForwardToZPlane(cam, zPlane);

        Vector3 w0 = cam.ViewportToWorldPoint(new Vector3(0f, 0f, dist));
        Vector3 w1 = cam.ViewportToWorldPoint(new Vector3(1f, 0f, dist));
        Vector3 w2 = cam.ViewportToWorldPoint(new Vector3(0f, 1f, dist));
        Vector3 w3 = cam.ViewportToWorldPoint(new Vector3(1f, 1f, dist));

        Vector3Int c0 = grid.WorldToCell(w0);
        Vector3Int c1 = grid.WorldToCell(w1);
        Vector3Int c2 = grid.WorldToCell(w2);
        Vector3Int c3 = grid.WorldToCell(w3);

        int minX = Mathf.Min(c0.x, c1.x, c2.x, c3.x) - 1;
        int maxX = Mathf.Max(c0.x, c1.x, c2.x, c3.x) + 1;
        int minY = Mathf.Min(c0.y, c1.y, c2.y, c3.y) - 1;
        int maxY = Mathf.Max(c0.y, c1.y, c2.y, c3.y) + 1;

        Vector2Int minTile = new Vector2Int(minX, minY);
        Vector2Int maxTile = new Vector2Int(maxX, maxY);

        minChunk = TileToChunk(minTile, prof.chunkSize);
        maxChunk = TileToChunk(maxTile, prof.chunkSize);
    }

    private static float DistanceAlongCameraForwardToZPlane(Camera cam, float zPlane)
    {
        Vector3 camPos = cam.transform.position;
        Vector3 fwd = cam.transform.forward;

        float denom = fwd.z;
        if (Mathf.Abs(denom) < 0.00001f)
            return cam.nearClipPlane;

        float t = (zPlane - camPos.z) / denom;
        if (t < 0f) t = -t;

        return Mathf.Max(cam.nearClipPlane, t);
    }

    private static Vector2Int TileToChunk(Vector2Int tile, int chunkSize)
    {
        int cx = FloorDiv(tile.x, chunkSize);
        int cy = FloorDiv(tile.y, chunkSize);
        return new Vector2Int(cx, cy);
    }

    private static int FloorDiv(int a, int b)
    {
        if (b == 0) return 0;

        int q = a / b;
        int r = a % b;

        if (r != 0 && ((r > 0) != (b > 0)))
            q--;

        return q;
    }
}
