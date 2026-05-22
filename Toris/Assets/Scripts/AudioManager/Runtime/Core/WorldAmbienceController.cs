using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class WorldAmbienceController : MonoBehaviour
{
    private const int MissingBiomeIndex = -1;

    [Header("Master")]
    [SerializeField] private bool ambienceEnabled = true;

    [Header("Scenes")]
    [SerializeField] private string mainAreaSceneName = "MainArea";
    [SerializeField] private string proceduralSceneName = "ProceduralTiles";

    [Header("Wind Ambience")]
    [SerializeField] private string mainAreaSfxId = "amb_wind";
    [SerializeField] private string defaultBiomeSfxId = "amb_wind";
    [SerializeField, Range(0f, 2f)] private float windVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float windFadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float windFadeOutSeconds = 1f;

    [Header("Forest Ambience")]
    [SerializeField] private int forestBiomeIndex = 1;
    [SerializeField] private string forestBiomeSfxId = "amb_forest";
    [SerializeField, Range(0f, 2f)] private float forestVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float forestFadeInSeconds = 1f;
    [SerializeField, Min(0f)] private float forestFadeOutSeconds = 1f;

    [Header("Water Ambience")]
    [SerializeField] private string waterSfxId = "amb_water";
    [SerializeField] private bool waterInAllBiomes = true;
    [SerializeField] private int waterBiomeIndex = 1;
    [SerializeField, Min(0)] private int waterProbeRadiusTiles = 10;
    [SerializeField, Range(0f, 1f)] private float waterOuterRingMinDistance01 = 0.6f;
    [SerializeField] private bool includeRenderedWaterTiles = true;
    [SerializeField, Min(0.05f)] private float waterProbeIntervalSeconds = 0.25f;
    [SerializeField, Range(0f, 2f)] private float waterVolumeMultiplier = 1f;
    [SerializeField, Min(0f)] private float waterFadeInSeconds = 0.7f;
    [SerializeField, Min(0f)] private float waterFadeOutSeconds = 0.7f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    [Header("Transition Suspension")]
    [SerializeField, Min(0f)] private float transitionFadeOutSeconds = 0.1f;

    private WorldGenRunner worldRunner;
    private AudioVoiceHandle windHandle = AudioVoiceHandle.Invalid;
    private AudioVoiceHandle forestHandle = AudioVoiceHandle.Invalid;
    private AudioVoiceHandle waterHandle = AudioVoiceHandle.Invalid;
    private Transform waterFollowTarget;
    private Transform fallbackFollowTarget;
    private string activeWindSfxId;
    private float nextWaterProbeTime;

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        RefreshWorldRunner();
        UpdateAmbience(forceWaterProbe: true);
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        StopWindAmbience();
        StopForestAmbience();
        StopWaterAmbience();
        worldRunner = null;
    }

    private void Update()
    {
        UpdateAmbience(forceWaterProbe: false);
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        worldRunner = null;
        fallbackFollowTarget = null;
        nextWaterProbeTime = 0f;
        UpdateAmbience(forceWaterProbe: true);
    }

    private void UpdateAmbience(bool forceWaterProbe)
    {
        if (AudioBootstrap.Sfx == null)
            return;

        if (!ambienceEnabled)
        {
            StopAllAmbience();
            return;
        }

        if (IsSceneTransitionLoading())
        {
            StopAllAmbience(transitionFadeOutSeconds);
            return;
        }

        RefreshWorldRunner();
        SetWindAmbience(ResolveWindAmbienceId());
        SetForestAmbience(ShouldPlayForestAmbience());

        if (forceWaterProbe || Time.unscaledTime >= nextWaterProbeTime)
        {
            nextWaterProbeTime = Time.unscaledTime + waterProbeIntervalSeconds;
            SetWaterAmbience(ShouldPlayWaterAmbience(), ResolveFollowTarget());
        }
    }

    private string ResolveWindAmbienceId()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (SceneNameEquals(activeScene.name, mainAreaSceneName))
            return mainAreaSfxId;

        return SceneNameEquals(activeScene.name, proceduralSceneName)
            ? defaultBiomeSfxId
            : null;
    }

    private bool ShouldPlayForestAmbience()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        bool shouldPlay = SceneNameEquals(activeScene.name, proceduralSceneName)
                          && ResolveCurrentBiomeIndex() == forestBiomeIndex
                          && !string.IsNullOrWhiteSpace(forestBiomeSfxId);

        DebugLogState($"Forest check: shouldPlay={shouldPlay}, scene={activeScene.name}, biome={ResolveCurrentBiomeIndex()}, targetBiome={forestBiomeIndex}, id={forestBiomeSfxId}");
        return shouldPlay;
    }

    private bool ShouldPlayWaterAmbience()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!SceneNameEquals(activeScene.name, proceduralSceneName))
        {
            DebugLogState($"Water check skipped: scene={activeScene.name}");
            return false;
        }

        if (worldRunner == null || string.IsNullOrWhiteSpace(waterSfxId))
        {
            DebugLogState($"Water check skipped: hasRunner={worldRunner != null}, id={waterSfxId}");
            return false;
        }

        WorldContext context = worldRunner.Context;
        if (context == null || (!waterInAllBiomes && context.ActiveBiome.Index != waterBiomeIndex))
        {
            int currentBiomeIndex = context != null ? context.ActiveBiome.Index : MissingBiomeIndex;
            DebugLogState($"Water check skipped: biome={currentBiomeIndex}, allBiomes={waterInAllBiomes}, targetBiome={waterBiomeIndex}");
            return false;
        }

        Transform followTarget = ResolveFollowTarget();
        if (followTarget == null)
        {
            DebugLogState("Water check skipped: follow target is missing");
            return false;
        }

        Vector2Int playerTile = worldRunner.WorldToTile(followTarget.position);
        Vector2Int localTile = context.ActiveBiome.ToLocal(playerTile);
        float dist01 = localTile.magnitude / Mathf.Max(1f, context.ActiveBiome.RadiusTiles);
        if (dist01 < waterOuterRingMinDistance01)
        {
            DebugLogState($"Water check skipped: dist01={dist01:0.00}, required={waterOuterRingMinDistance01:0.00}, tile={playerTile}");
            return false;
        }

        bool hasOuterRingWater = HasOuterRingWaterNear(context, playerTile, waterProbeRadiusTiles, out Vector2Int nearestOuterRingTile);
        Vector2Int nearestRenderedWaterTile = default;
        bool hasRenderedWater = includeRenderedWaterTiles
                                && HasRenderedWaterNear(worldRunner, playerTile, waterProbeRadiusTiles, out nearestRenderedWaterTile);
        bool hasWater = hasOuterRingWater || hasRenderedWater;

        DebugLogState(
            $"Water check: hasWater={hasWater}, outerMask={hasOuterRingWater}, rendered={hasRenderedWater}, " +
            $"dist01={dist01:0.00}, radius={waterProbeRadiusTiles}, tile={playerTile}, " +
            $"nearestOuter={FormatTile(nearestOuterRingTile)}, nearestRendered={FormatTile(nearestRenderedWaterTile)}");
        return hasWater;
    }

    private void SetWindAmbience(string sfxId)
    {
        if (string.Equals(activeWindSfxId, sfxId))
            return;

        StopWindAmbience();

        if (string.IsNullOrWhiteSpace(sfxId))
            return;

        // SFX-only hook: scene state selects the passive wind bed without mutating world state.
        SfxPlayRequest request = MakeLoopRequest(windVolumeMultiplier, windFadeInSeconds, force2D: true);
        windHandle = AudioBootstrap.Sfx.PlayLoop(sfxId, Vector3.zero, request);
        activeWindSfxId = windHandle.IsValid ? sfxId : null;
        DebugLogState($"Wind play requested: id={sfxId}, handle={windHandle}");
    }

    private void SetForestAmbience(bool shouldPlay)
    {
        if (!shouldPlay)
        {
            StopForestAmbience();
            return;
        }

        if (forestHandle.IsValid)
            return;

        // SFX-only hook: forest ambience layers over wind while the active procedural biome is the forest biome.
        SfxPlayRequest request = MakeLoopRequest(forestVolumeMultiplier, forestFadeInSeconds, force2D: true);
        forestHandle = AudioBootstrap.Sfx.PlayLoop(forestBiomeSfxId, Vector3.zero, request);
        DebugLogState($"Forest play requested: id={forestBiomeSfxId}, handle={forestHandle}");
    }

    private void SetWaterAmbience(bool shouldPlay, Transform followTarget)
    {
        if (!shouldPlay || followTarget == null)
        {
            StopWaterAmbience();
            return;
        }

        if (waterHandle.IsValid && waterFollowTarget == followTarget)
            return;

        StopWaterAmbience();

        // SFX-only hook: water ambience is proximity-gated near the outer water ring and follows the player as an audio bed.
        SfxPlayRequest request = MakeLoopRequest(waterVolumeMultiplier, waterFadeInSeconds, force2D: true);
        waterHandle = AudioBootstrap.Sfx.PlayAttachedLoop(waterSfxId, followTarget, Vector3.zero, request);
        waterFollowTarget = waterHandle.IsValid ? followTarget : null;
        DebugLogState($"Water play requested: id={waterSfxId}, handle={waterHandle}, target={followTarget.name}");
    }

    private void StopWindAmbience(float? fadeOutOverrideSeconds = null)
    {
        if (windHandle.IsValid && AudioBootstrap.Sfx != null)
            AudioBootstrap.Sfx.Stop(windHandle, fadeOutOverrideSeconds ?? windFadeOutSeconds);

        if (windHandle.IsValid)
            DebugLogState($"Wind stop requested: id={activeWindSfxId}");

        windHandle = AudioVoiceHandle.Invalid;
        activeWindSfxId = null;
    }

    private void StopForestAmbience(float? fadeOutOverrideSeconds = null)
    {
        if (forestHandle.IsValid && AudioBootstrap.Sfx != null)
            AudioBootstrap.Sfx.Stop(forestHandle, fadeOutOverrideSeconds ?? forestFadeOutSeconds);

        if (forestHandle.IsValid)
            DebugLogState($"Forest stop requested: id={forestBiomeSfxId}");

        forestHandle = AudioVoiceHandle.Invalid;
    }

    private void StopWaterAmbience(float? fadeOutOverrideSeconds = null)
    {
        if (waterHandle.IsValid && AudioBootstrap.Sfx != null)
            AudioBootstrap.Sfx.Stop(waterHandle, fadeOutOverrideSeconds ?? waterFadeOutSeconds);

        if (waterHandle.IsValid)
            DebugLogState($"Water stop requested: id={waterSfxId}");

        waterHandle = AudioVoiceHandle.Invalid;
        waterFollowTarget = null;
    }

    private void StopAllAmbience(float? fadeOutOverrideSeconds = null)
    {
        StopWindAmbience(fadeOutOverrideSeconds);
        StopForestAmbience(fadeOutOverrideSeconds);
        StopWaterAmbience(fadeOutOverrideSeconds);
    }

    private void RefreshWorldRunner()
    {
        if (!SceneNameEquals(SceneManager.GetActiveScene().name, proceduralSceneName))
        {
            worldRunner = null;
            return;
        }

        if (worldRunner != null && worldRunner.isActiveAndEnabled)
            return;

        worldRunner = FindFirstObjectByType<WorldGenRunner>();
    }

    private int ResolveCurrentBiomeIndex()
    {
        WorldContext context = worldRunner != null ? worldRunner.Context : null;
        return context != null ? context.ActiveBiome.Index : MissingBiomeIndex;
    }

    private Transform ResolveFollowTarget()
    {
        if (worldRunner != null && worldRunner.FollowTarget != null)
            return worldRunner.FollowTarget;

        if (fallbackFollowTarget != null)
            return fallbackFollowTarget;

        // SFX-only fallback: ambience can follow the player even when WorldGenRunner.followTarget is not assigned in the scene.
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            fallbackFollowTarget = playerController.transform;
            return fallbackFollowTarget;
        }

        PlayerMotor playerMotor = FindFirstObjectByType<PlayerMotor>();
        if (playerMotor != null)
        {
            fallbackFollowTarget = playerMotor.transform;
            return fallbackFollowTarget;
        }

        return null;
    }

    private static bool HasOuterRingWaterNear(
        WorldContext context,
        Vector2Int centerTile,
        int radiusTiles,
        out Vector2Int nearestWaterTile)
    {
        nearestWaterTile = default;
        if (context == null)
            return false;

        int radius = Mathf.Max(0, radiusTiles);
        int radiusSqr = radius * radius;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                if (offset.sqrMagnitude > radiusSqr)
                    continue;

                Vector2Int tile = centerTile + offset;
                Vector2Int localTile = context.ActiveBiome.ToLocal(tile);
                if (!context.Mask.IsLand(localTile, context))
                {
                    nearestWaterTile = tile;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasRenderedWaterNear(
        WorldGenRunner runner,
        Vector2Int centerTile,
        int radiusTiles,
        out Vector2Int nearestWaterTile)
    {
        nearestWaterTile = default;
        if (runner == null)
            return false;

        int radius = Mathf.Max(0, radiusTiles);
        int radiusSqr = radius * radius;
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                Vector2Int offset = new Vector2Int(x, y);
                if (offset.sqrMagnitude > radiusSqr)
                    continue;

                Vector2Int tile = centerTile + offset;
                if (!runner.HasRenderedWaterTile(tile))
                    continue;

                nearestWaterTile = tile;
                return true;
            }
        }

        return false;
    }

    private static SfxPlayRequest MakeLoopRequest(float volumeMultiplier, float fadeInSeconds, bool force2D)
    {
        SfxPlayRequest request = SfxPlayRequest.Default;
        request.volumeMultiplier = volumeMultiplier;
        request.fadeInSeconds = fadeInSeconds;
        request.force2D = force2D;
        return request;
    }

    private static bool SceneNameEquals(string a, string b)
    {
        return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatTile(Vector2Int tile)
    {
        return $"({tile.x}, {tile.y})";
    }

    private static bool IsSceneTransitionLoading()
    {
        SceneTransitionService transitionService = SceneTransitionService.Instance;
        return transitionService != null && transitionService.IsLoading;
    }

    private void DebugLogState(string message)
    {
#if UNITY_EDITOR
        if (!debugLogs)
            return;

        Debug.Log($"[WorldAmbienceController] {message}", this);
#endif
    }
}
