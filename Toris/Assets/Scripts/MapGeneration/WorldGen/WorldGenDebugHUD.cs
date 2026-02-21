using System.Collections.Generic;
using System.Reflection;
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

    // ---------- reflection ----------
    private static bool reflectionBound;
    private static FieldInfo f_loaded;
    private static FieldInfo f_preload;
    private static FieldInfo f_hyst;
    private static FieldInfo f_streamCam;
    private static FieldInfo f_profile;

    private static void BindReflection()
    {
        if (reflectionBound) return;
        reflectionBound = true;

        var t = typeof(WorldGenRunner);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        f_loaded = t.GetField("loaded", flags);
        f_preload = t.GetField("preloadChunks", flags);
        f_hyst = t.GetField("unloadHysteresisChunks", flags);
        f_streamCam = t.GetField("streamCamera", flags);
        f_profile = t.GetField("profile", flags);
    }

    private HashSet<Vector2Int> TryGetLoaded()
    {
        BindReflection();
        return f_loaded?.GetValue(runner) as HashSet<Vector2Int>;
    }

    private int TryGetPreloadChunks()
    {
        BindReflection();
        return f_preload?.GetValue(runner) is int i ? i : 0;
    }

    private int TryGetUnloadHysteresisChunks()
    {
        BindReflection();
        return f_hyst?.GetValue(runner) is int i ? i : 0;
    }

    private Camera TryGetStreamCamera()
    {
        BindReflection();
        var cam = f_streamCam?.GetValue(runner) as Camera;
        return cam != null ? cam : Camera.main;
    }

    private WorldProfile TryGetProfile()
    {
        BindReflection();
        return f_profile?.GetValue(runner) as WorldProfile;
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
        GUI.Box(r, $"WorldGen Debug ({toggleKey})");

        GUILayout.BeginArea(new Rect(r.x + 10, r.y + 24, r.width - 20, r.height - 34));

        GUILayout.Label("Visuals:", style);
        drawChunkBorders = GUILayout.Toggle(drawChunkBorders, " Chunk borders");
        drawStreamingRects = GUILayout.Toggle(drawStreamingRects, " Streaming rects (load/unload)");
        GUILayout.Space(6);

        GUILayout.Label($"Biome: {biomeName} (idx {ctx.ActiveBiome.Index})", style);
        GUILayout.Label($"Tile: {focusTile}", style);
        GUILayout.Label($"dist01: {s.dist01:F2}", style);
        GUILayout.Label($"danger01: {s.danger01:F2}", style);
        GUILayout.Label($"veg01: {s.vegetation01:F2}", style);
        GUILayout.Label($"lake01: {s.lake01:F2}", style);

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
        if (!visible) return;
        if (!drawChunkBorders && !drawStreamingRects) return;
        if (runner == null || runner.Context == null) return;
        if (grid == null) return;
        if (cam == null) return;
        if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView) return;


        WorldProfile prof = TryGetProfile();
        if (prof == null) return;

        EnsureLineMaterial();
        if (lineMat == null) return;

        lineMat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);

        int chunkSize = prof.chunkSize;

        if (drawChunkBorders)
        {
            var loaded = TryGetLoaded();
            if (loaded != null)
            {
                GL.Color(new Color(0f, 1f, 0f, 0.75f));
                foreach (var c in loaded)
                    DrawChunkRectGL(c, chunkSize);
            }
        }

        if (drawStreamingRects)
        {
            Camera streamCam = TryGetStreamCamera();
            if (streamCam != null)
            {
                GetCameraChunkRect(streamCam, prof, out var loadMinChunk, out var loadMaxChunk);

                int pad = Mathf.Max(0, prof.viewDistanceChunks) + Mathf.Max(0, TryGetPreloadChunks());
                loadMinChunk -= new Vector2Int(pad, pad);
                loadMaxChunk += new Vector2Int(pad, pad);

                int hyst = Mathf.Max(0, TryGetUnloadHysteresisChunks());
                Vector2Int unloadMin = loadMinChunk - new Vector2Int(hyst, hyst);
                Vector2Int unloadMax = loadMaxChunk + new Vector2Int(hyst, hyst);

                GL.Color(new Color(0f, 0.6f, 1f, 0.9f));
                DrawChunkRangeRectGL(loadMinChunk, loadMaxChunk, chunkSize);

                GL.Color(new Color(1f, 0.6f, 0f, 0.9f));
                DrawChunkRangeRectGL(unloadMin, unloadMax, chunkSize);
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
