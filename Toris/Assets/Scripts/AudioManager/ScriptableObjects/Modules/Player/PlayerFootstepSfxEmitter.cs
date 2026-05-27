using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public sealed class PlayerFootstepSfxEmitter : MonoBehaviour
{
    private const float NormalMovementMultiplier = 1f;

    [Header("Dependencies")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerStats stats;
    [SerializeField] private FootstepSurfaceMap surfaceMap;

    [Header("Step Cadence")]
    [SerializeField, Min(0.01f)] private float distancePerStep = 0.8f;
    [SerializeField, Min(0f)] private float firstStepDistance = 0.18f;
    [SerializeField, Min(0f)] private float movementSpeedThreshold = 0.1f;

    [Header("Boosted Movement Cadence")]
    [Tooltip("How strongly movement speed above 1x increases audible step cadence. At 0.7, a 2x speed effect targets 1.7x cadence before the cap.")]
    [SerializeField, Range(0f, 1f)] private float boostedCadenceInfluence = 0.7f;
    [Tooltip("Maximum audible footstep cadence multiplier during movement-speed buffs.")]
    [SerializeField, Min(1f)] private float maxBoostedCadenceMultiplier = 1.7f;

    [Header("Surface Sampling")]
    [SerializeField] private Vector3 tileSampleOffset;
    [SerializeField] private string fallbackSfxId = "player_footstep_leaf";

    [Header("Request")]
    [SerializeField] private bool force2D = true;

    [Header("Diagnostics")]
    [SerializeField] private bool debugLogSteps;

    private Tilemap[] surfaceTilemaps = System.Array.Empty<Tilemap>();
    private Vector3 previousPosition;
    private float remainingStepDistance;
    private bool wasMoving;

    private void Awake()
    {
        ResolveDependencies();
    }

    private void OnEnable()
    {
        ResolveDependencies();
        SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        CacheSurfaceTilemaps();
        ResetCadence();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
        ResetCadence();
    }

    private void Update()
    {
        Vector3 currentPosition = transform.position;
        float movedDistance = Vector2.Distance(currentPosition, previousPosition);
        previousPosition = currentPosition;

        if (!CanEmitFootsteps())
        {
            wasMoving = false;
            remainingStepDistance = Mathf.Max(0f, firstStepDistance);
            return;
        }

        if (!wasMoving)
        {
            wasMoving = true;
            remainingStepDistance = Mathf.Min(firstStepDistance, GetEffectiveDistancePerStep());
        }

        remainingStepDistance -= movedDistance;
        if (remainingStepDistance > 0f)
            return;

        PlayStep(currentPosition);
        remainingStepDistance += GetEffectiveDistancePerStep();
    }

    private void OnValidate()
    {
        ResolveDependencies();
        distancePerStep = Mathf.Max(0.01f, distancePerStep);
        firstStepDistance = Mathf.Max(0f, firstStepDistance);
        movementSpeedThreshold = Mathf.Max(0f, movementSpeedThreshold);
        boostedCadenceInfluence = Mathf.Clamp01(boostedCadenceInfluence);
        maxBoostedCadenceMultiplier = Mathf.Max(NormalMovementMultiplier, maxBoostedCadenceMultiplier);
    }

    private void HandleActiveSceneChanged(Scene previousScene, Scene nextScene)
    {
        CacheSurfaceTilemaps();
        ResetCadence();
    }

    private void ResolveDependencies()
    {
        if (rb == null)
            TryGetComponent(out rb);

        if (motor == null)
            TryGetComponent(out motor);

        if (stats == null)
            TryGetComponent(out stats);
    }

    private bool CanEmitFootsteps()
    {
        if (AudioBootstrap.Sfx == null || rb == null || Time.timeScale <= 0f)
            return false;

        if (motor != null && motor.isDashing)
            return false;

#if UNITY_2022_1_OR_NEWER
        Vector2 velocity = rb.linearVelocity;
#else
        Vector2 velocity = rb.velocity;
#endif
        return velocity.sqrMagnitude > movementSpeedThreshold * movementSpeedThreshold;
    }

    private float GetEffectiveDistancePerStep()
    {
        float baseDistancePerStep = Mathf.Max(0.01f, distancePerStep);
        if (stats == null)
            return baseDistancePerStep;

        float movementMultiplier = Mathf.Max(0f, stats.ResolvedEffects.moveSpeedMultiplier);
        if (movementMultiplier <= NormalMovementMultiplier)
            return baseDistancePerStep;

        // SFX-only tuning: boosted travel remains unchanged while audible footfalls scale more gently.
        float uncappedCadenceMultiplier = NormalMovementMultiplier
            + ((movementMultiplier - NormalMovementMultiplier) * Mathf.Clamp01(boostedCadenceInfluence));
        float cadenceMultiplier = Mathf.Min(
            Mathf.Max(NormalMovementMultiplier, maxBoostedCadenceMultiplier),
            uncappedCadenceMultiplier);

        return baseDistancePerStep * movementMultiplier / cadenceMultiplier;
    }

    private void PlayStep(Vector3 worldPosition)
    {
        TileBase sampledTile = ResolveMappedTile(out string sfxId);
        if (string.IsNullOrWhiteSpace(sfxId))
            return;

        // SFX-only hook: footsteps read movement and painted terrain without mutating gameplay state.
        SfxPlayRequest request = SfxPlayRequest.Default;
        request.force2D = force2D;
        AudioBootstrap.Sfx.PlayAt(sfxId, worldPosition, request);

#if UNITY_EDITOR
        if (debugLogSteps)
        {
            string tileName = sampledTile != null ? sampledTile.name : "fallback";
            Debug.Log($"[PlayerFootstepSfxEmitter] Step id={sfxId}, tile={tileName}, world={worldPosition}.", this);
        }
#endif
    }

    private TileBase ResolveMappedTile(out string sfxId)
    {
        Vector3 samplePosition = transform.position + tileSampleOffset;

        if (surfaceMap != null)
        {
            for (int i = 0; i < surfaceTilemaps.Length; i++)
            {
                Tilemap tilemap = surfaceTilemaps[i];
                if (tilemap == null)
                    continue;

                TileBase tile = tilemap.GetTile(tilemap.WorldToCell(samplePosition));
                if (surfaceMap.TryResolveSfxId(tile, out sfxId))
                    return tile;
            }

            sfxId = surfaceMap.FallbackSfxId;
            return null;
        }

        sfxId = fallbackSfxId;
        return null;
    }

    private void CacheSurfaceTilemaps()
    {
        surfaceTilemaps = FindObjectsByType<Tilemap>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
    }

    private void ResetCadence()
    {
        previousPosition = transform.position;
        remainingStepDistance = Mathf.Max(0f, firstStepDistance);
        wasMoving = false;
    }
}
