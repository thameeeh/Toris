using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public sealed class WorldGenDebugHUD : MonoBehaviour
{
    private const float MinimumPanelHeightDefault = 330f;
    private const float MinimumPanelHeightWithAdvancedDiagnostics = 510f;

    [Header("References")]
    [SerializeField] private WorldGenRunner runner;
    [SerializeField] private Grid grid;
    [SerializeField] private Transform followTarget;

    [Header("UI")]
    [SerializeField] private bool visible = true;
    [SerializeField] private Vector2 panelPos = new Vector2(12, 12);
    [SerializeField] private Vector2 panelSize = new Vector2(380, 330);
    [SerializeField] private int fontSize = 14;
    [SerializeField] private bool showAdvancedStats = false;

    [Header("Gameplay Debug Visuals")]
    [SerializeField] private bool drawChunkBorders = true;
    [SerializeField] private bool drawStreamingRects = false;
    [SerializeField] private float zOffset = -0.1f;

    [SerializeField] private KeyCode toggleKey = KeyCode.F3;

    private readonly TileResolver resolver = new TileResolver();
    private GUIStyle style;
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

        WorldGenDiagnosticsSnapshot diagnosticsSnapshot = runner.CreateDiagnosticsSnapshot();
        StreamingDiagnosticsSnapshot streamingDiagnostics = diagnosticsSnapshot.Streaming;
        LifecycleDiagnosticsSnapshot lifecycleDiagnostics = diagnosticsSnapshot.Lifecycle;
        BuildOutputDiagnosticsSnapshot buildOutputDiagnostics = diagnosticsSnapshot.BuildOutputDiagnostics;
        NavigationDiagnosticsSnapshot navigationDiagnostics = diagnosticsSnapshot.Navigation;
        TransitionDiagnosticsSnapshot transitionDiagnostics = diagnosticsSnapshot.Transition;

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

        float minimumPanelHeight = showAdvancedStats
            ? MinimumPanelHeightWithAdvancedDiagnostics
            : MinimumPanelHeightDefault;
        float panelHeight = Mathf.Max(panelSize.y, minimumPanelHeight);
        Rect panelRect = new Rect(panelPos.x, panelPos.y, panelSize.x, panelHeight);
        GUI.Box(panelRect, $"WorldGen Debug ({toggleKey})");

        GUILayout.BeginArea(new Rect(panelRect.x + 10f, panelRect.y + 24f, panelRect.width - 20f, panelRect.height - 34f));

        GUILayout.Label("Visuals:", style);
        drawChunkBorders = GUILayout.Toggle(drawChunkBorders, " Chunk borders");
        drawStreamingRects = GUILayout.Toggle(drawStreamingRects, " Streaming rects (load/unload)");
        showAdvancedStats = GUILayout.Toggle(showAdvancedStats, " Advanced stats");
        GUILayout.Space(6f);

        GUILayout.Label($"Biome: {biomeName}", style);
        GUILayout.Label($"Tile: {focusTile}", style);
        GUILayout.Label($"dist/danger: {worldSignals.dist01:F2} / {worldSignals.danger01:F2}", style);
        GUILayout.Label($"veg/lake: {worldSignals.vegetation01:F2} / {worldSignals.lake01:F2}", style);

        if (showAdvancedStats)
        {
            GUILayout.Space(6f);

            GUILayout.Label("Streaming:", style);
            GUILayout.Label($"Loaded chunks: {streamingDiagnostics.LoadedChunkCount}", style);
            GUILayout.Label($"Queue entries: {streamingDiagnostics.GenerationQueueCount}", style);
            GUILayout.Label($"Queued chunks: {streamingDiagnostics.QueuedChunkCount}", style);
            GUILayout.Label(
                streamingDiagnostics.StreamingAnchorInitialized
                    ? $"Streaming anchor: {streamingDiagnostics.StreamingAnchorChunk}"
                    : "Streaming anchor: (uninitialized)",
                style);
            GUILayout.Label($"Preload chunks: {streamingDiagnostics.PreloadChunks}", style);
            GUILayout.Label($"Unload hysteresis: {streamingDiagnostics.UnloadHysteresisChunks}", style);
            GUILayout.Space(6f);

            GUILayout.Label("Lifecycle:", style);
            GUILayout.Label($"Active site chunks: {lifecycleDiagnostics.ActiveSiteChunkCount}", style);
            GUILayout.Label($"Persistent sites: {lifecycleDiagnostics.ActivePersistentSiteCount}", style);
            GUILayout.Label($"Active sites total: {lifecycleDiagnostics.ActiveSiteCount}", style);
            GUILayout.Label($"Placed sites: {lifecycleDiagnostics.TotalPlacedSiteCount}", style);
            GUILayout.Space(6f);

            GUILayout.Label("Build Output:", style);
            GUILayout.Label($"Build overrides: {buildOutputDiagnostics.TerrainOverrideCount}", style);
            GUILayout.Label($"Build placements: {buildOutputDiagnostics.TotalPlacementCount}", style);
            GUILayout.Label($"Chunk placements: {buildOutputDiagnostics.ChunkPlacementCount}", style);
            GUILayout.Label($"Persistent placements: {buildOutputDiagnostics.PersistentPlacementCount}", style);
            GUILayout.Label($"Road anchors: {buildOutputDiagnostics.RoadAnchorCount}", style);
            GUILayout.Space(6f);

            GUILayout.Label("Navigation:", style);
            GUILayout.Label($"Nav contributions: {buildOutputDiagnostics.NavigationContributionCount}", style);
            GUILayout.Label($"Nav chunks: {navigationDiagnostics.LoadedNavChunkCount}", style);
            GUILayout.Label(
                navigationDiagnostics.NavigationContributionsBound
                    ? "Nav contributions: bound"
                    : "Nav contributions: (unbound)",
                style);
            GUILayout.Space(6f);

            GUILayout.Label("Transitions:", style);
            GUILayout.Label($"Biome idx: {transitionDiagnostics.CurrentBiomeIndex}", style);
            GUILayout.Label($"Gate cooldown: {transitionDiagnostics.GateCooldownRemainingSeconds:F2}s", style);
            GUILayout.Label(
                transitionDiagnostics.SceneTransitionLoading
                    ? "Scene transition: loading"
                    : "Scene transition: idle",
                style);
        }

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

        WorldGenDiagnosticsSnapshot diagnosticsSnapshot = runner.CreateDiagnosticsSnapshot();
        StreamingDiagnosticsSnapshot streamingDiagnostics = diagnosticsSnapshot.Streaming;

        if (streamingDiagnostics.ChunkSize <= 0)
            return;

        EnsureLineMaterial();
        if (lineMat == null)
            return;

        lineMat.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(Matrix4x4.identity);
        GL.Begin(GL.LINES);

        int chunkSize = streamingDiagnostics.ChunkSize;

        if (drawChunkBorders)
        {
            IReadOnlyCollection<Vector2Int> loadedChunks = streamingDiagnostics.LoadedChunks;
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
            if (streamingDiagnostics.HasStreamingBounds)
            {
                ChunkStreamingBounds streamingBounds = streamingDiagnostics.StreamingBounds;

                GL.Color(new Color(0f, 0.6f, 1f, 0.9f));
                DrawChunkRangeRectGL(
                    streamingBounds.LoadMinChunk,
                    streamingBounds.LoadMaxChunk,
                    chunkSize);

                GL.Color(new Color(1f, 0.6f, 0f, 0.9f));
                DrawChunkRangeRectGL(
                    streamingBounds.UnloadMinChunk,
                    streamingBounds.UnloadMaxChunk,
                    chunkSize);
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
}
