using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public sealed class NecromancerGraveSite : MonoBehaviour, IInteractable, IPoolable, IWorldSiteBridge
{
    private const string EncounterActiveStateKey = "necromancer_encounter_active";

    private Collider2D interactionCollider;
    private PlayerInteractor currentInteractor;
    private WorldSiteStateHandle worldSiteState;
    private IEnemySpawnService enemySpawnService;
    private NecromancerGraveEncounterConfig encounterConfig;
    private bool interactionAvailable;
    private bool isPlayerInsideTrigger;
    private bool encounterStarted;
    private Coroutine pendingSpawnRoutine;
    private Necromancer awakenedNecromancer;

    private void Awake()
    {
        TryGetComponent(out interactionCollider);
        EnsureTriggerCollider();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (interactionCollider == null)
            TryGetComponent(out interactionCollider);

        EnsureTriggerCollider();
    }
#endif

    public void Initialize(WorldSiteContext siteContext)
    {
        encounterConfig = siteContext.GetRuntimeConfig<NecromancerGraveEncounterConfig>();
        enemySpawnService = siteContext.EncounterServices?.EnemySpawnService;
        worldSiteState = siteContext.WorldSiteStateService != null
            ? siteContext.WorldSiteStateService.GetSiteState(siteContext.Placement.ChunkCoord, siteContext.SpawnId)
            : default;

        bool encounterActive = worldSiteState.GetBool(EncounterActiveStateKey);
        SetInteractionAvailable(!worldSiteState.IsConsumed && !encounterActive);
    }

    public void Interact(GameObject interactor)
    {
        if (!interactionAvailable || pendingSpawnRoutine != null)
            return;

        if (encounterConfig == null || encounterConfig.NecromancerPrefab == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{nameof(NecromancerGraveSite)} is missing a {nameof(NecromancerGraveEncounterConfig)} or Necromancer prefab.", this);
#endif
            return;
        }

        if (enemySpawnService == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{nameof(NecromancerGraveSite)} has no {nameof(IEnemySpawnService)}.", this);
#endif
            return;
        }

        SetInteractionAvailable(false);
        pendingSpawnRoutine = StartCoroutine(SpawnNecromancerAfterDelay());
    }

    public void OnSpawned()
    {
        StopPendingSpawnRoutine();
        UnbindAwakenedNecromancer();
        currentInteractor = null;
        encounterConfig = null;
        enemySpawnService = null;
        worldSiteState = default;
        isPlayerInsideTrigger = false;
        encounterStarted = false;
        SetInteractionAvailable(false);
        EnsureTriggerCollider();
    }

    public void OnDespawned()
    {
        StopPendingSpawnRoutine();
        UnbindAwakenedNecromancer();
        ClearCurrentInteractor();
        encounterConfig = null;
        enemySpawnService = null;
        worldSiteState = default;
        isPlayerInsideTrigger = false;
        encounterStarted = false;
        SetInteractionAvailable(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerInteractor playerInteractor))
            return;

        isPlayerInsideTrigger = true;

        if (awakenedNecromancer != null && !encounterStarted)
            return;

        if (!interactionAvailable || pendingSpawnRoutine != null)
            return;

        currentInteractor = playerInteractor;
        playerInteractor.SetCurrent(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerInteractor playerInteractor))
            return;

        isPlayerInsideTrigger = false;

        if (awakenedNecromancer != null
            && !encounterStarted
            && encounterConfig != null
            && encounterConfig.BeginEncounterWhenPlayerLeavesGrave)
        {
            BeginEncounter();
        }

        if (currentInteractor == playerInteractor)
            currentInteractor = null;

        playerInteractor.ClearCurrent(this);
    }

    private void SetInteractionAvailable(bool available)
    {
        interactionAvailable = available;
        if (!interactionAvailable)
            ClearCurrentInteractor();
    }

    private IEnumerator SpawnNecromancerAfterDelay()
    {
        float spawnDelaySeconds = encounterConfig != null ? encounterConfig.SpawnDelaySeconds : 0f;
        if (spawnDelaySeconds > 0f)
            yield return new WaitForSeconds(spawnDelaySeconds);

        pendingSpawnRoutine = null;

        if (encounterConfig == null || encounterConfig.NecromancerPrefab == null || enemySpawnService == null)
        {
            SetInteractionAvailable(true);
            yield break;
        }

        Vector3 spawnPosition = transform.position + (Vector3)encounterConfig.SpawnOffset;
        Enemy spawnedEnemy = enemySpawnService.SpawnEnemy(encounterConfig.NecromancerPrefab, spawnPosition, Quaternion.identity);
        Necromancer spawnedNecromancer = spawnedEnemy as Necromancer;
        if (spawnedNecromancer == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning($"{nameof(NecromancerGraveSite)} failed to spawn a {nameof(Necromancer)} from the grave interaction.", this);
#endif
            SetInteractionAvailable(true);
            yield break;
        }

        worldSiteState.SetBool(EncounterActiveStateKey, true);
        awakenedNecromancer = spawnedNecromancer;
        encounterStarted = false;

        awakenedNecromancer.AlwaysAggroed = false;
        awakenedNecromancer.SetAggroStatus(false);
        awakenedNecromancer.SetMovementAnimation(false);
        awakenedNecromancer.FacePlayer();
        awakenedNecromancer.Damaged += HandleAwakenedNecromancerDamaged;
        awakenedNecromancer.Despawned += HandleAwakenedNecromancerDespawned;

        NecromancerGraveEncounterRelay relay = spawnedNecromancer.GetComponent<NecromancerGraveEncounterRelay>();
        if (relay == null)
            relay = spawnedNecromancer.gameObject.AddComponent<NecromancerGraveEncounterRelay>();

        relay.Bind(worldSiteState, EncounterActiveStateKey);

        if (!isPlayerInsideTrigger)
            BeginEncounter();
    }

    private void HandleAwakenedNecromancerDamaged(float damageAmount)
    {
        if (damageAmount <= 0f)
            return;

        BeginEncounter();
    }

    private void HandleAwakenedNecromancerDespawned(Enemy despawnedEnemy)
    {
        if (despawnedEnemy != awakenedNecromancer)
            return;

        UnbindAwakenedNecromancer();
    }

    private void BeginEncounter()
    {
        if (awakenedNecromancer == null || encounterStarted)
            return;

        encounterStarted = true;
        awakenedNecromancer.AlwaysAggroed = true;
        awakenedNecromancer.SetAggroStatus(true);
        awakenedNecromancer.FacePlayer();

        if (encounterConfig != null && encounterConfig.TransformToFloaterOnEncounterStart)
            awakenedNecromancer.RequestBecomeFloater();
    }

    private void ClearCurrentInteractor()
    {
        if (currentInteractor == null)
            return;

        currentInteractor.ClearCurrent(this);
        currentInteractor = null;
    }

    private void EnsureTriggerCollider()
    {
        if (interactionCollider != null)
            interactionCollider.isTrigger = true;
    }

    private void StopPendingSpawnRoutine()
    {
        if (pendingSpawnRoutine == null)
            return;

        StopCoroutine(pendingSpawnRoutine);
        pendingSpawnRoutine = null;
    }

    private void UnbindAwakenedNecromancer()
    {
        if (awakenedNecromancer == null)
            return;

        awakenedNecromancer.Damaged -= HandleAwakenedNecromancerDamaged;
        awakenedNecromancer.Despawned -= HandleAwakenedNecromancerDespawned;
        awakenedNecromancer = null;
    }
}
