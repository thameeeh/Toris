using UnityEngine;

public sealed class WolfDenSpawner : MonoBehaviour, IPoolable, IWorldSiteContextConsumer
{
    [SerializeField] private MonoBehaviour encounterSiteComponent;

    private WorldEncounterPackage encounterPackage;
    private WolfDenEncounterConfig encounterConfig;
    private WorldEncounterOccupantPolicy occupantPolicy;
    private IWorldEncounterSite denSite;
    private WorldEncounterServices encounterServices;
    private readonly WorldEncounterPackageBinding packageBinding = new();

    private bool ready;
    private Wolf leader;
    private PackController pack;
    private readonly WorldEncounterOccupantCollection<Wolf> occupants = new();
    private float respawnTimer;

    private float alertLevel;
    private float alertDecayDelayTimer;
    private bool hasTriggeredMaxAlertHowl;

    public void Initialize(WorldSiteContext siteContext)
    {
        if (siteContext.TryGetEncounterPackage(out encounterPackage))
        {
            encounterConfig = encounterPackage.GetConfig<WolfDenEncounterConfig>();
            occupantPolicy = encounterPackage.OccupantPolicy;
            encounterServices = encounterPackage.Services;
            return;
        }

        encounterPackage = default;
        encounterConfig = siteContext.GetRuntimeConfig<WolfDenEncounterConfig>();
        occupantPolicy = encounterConfig != null ? encounterConfig.OccupantPolicy : null;
        encounterServices = siteContext.EncounterServices;
    }

    private void Awake()
    {
        denSite = ResolveDenSite();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (encounterSiteComponent is IWorldEncounterSite)
            return;

        encounterSiteComponent = FindEncounterSiteComponent();
    }
#endif

    private void OnEnable()
    {
        ResetRuntime();
        SubscribeToDen();
    }

    private void OnDisable()
    {
        UnsubscribeFromDen();
    }

    public void OnSpawned()
    {
        ResetRuntime();
        SubscribeToDen();
    }

    public void OnDespawned()
    {
        UnsubscribeFromDen();

        if (occupantPolicy != null && occupantPolicy.KeepOccupantsOnUnloadIfChasingPlayer)
            DespawnTrackedExceptActiveChasers();
        else
            ForceDespawnAllTracked();

        encounterServices = null;
        ResetRuntime();
    }

    private void ResetRuntime()
    {
        ready = false;
        occupants.Clear();
        leader = null;
        pack = null;
        respawnTimer = 0f;

        alertLevel = 0f;
        alertDecayDelayTimer = 0f;
        hasTriggeredMaxAlertHowl = false;
    }

    private void SubscribeToDen()
    {
        if (denSite == null)
            denSite = ResolveDenSite();

        packageBinding.Bind(
            denSite,
            encounterPackage,
            HandleDenInitialized,
            HandleDenCleared,
            OnDenDamaged);
    }

    private void UnsubscribeFromDen()
    {
        packageBinding.Unbind();
    }

    private void HandleDenInitialized()
    {
        if (denSite == null)
            denSite = ResolveDenSite();

        if (!HasEncounterConfig())
            return;

        ready = true;

        if (denSite != null && !denSite.IsCleared)
            EnsureLeader();
    }

    private void Update()
    {
        if (!ready) return;
        if (denSite == null) return;
        if (denSite.IsCleared) return;

        occupants.RemoveNulls();

        if (leader == null)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= occupantPolicy.RespawnDelay)
            {
                respawnTimer = 0f;
                EnsureLeader();
            }
        }

        if (alertDecayDelayTimer > 0f)
        {
            alertDecayDelayTimer -= Time.deltaTime;
        }
        else if (alertLevel > 0f)
        {
            alertLevel = Mathf.Max(0f, alertLevel - encounterConfig.AlertLevelDecayRate * Time.deltaTime);
        }

        if (alertLevel < encounterConfig.MaxAlertLevel)
        {
            hasTriggeredMaxAlertHowl = false;
        }
    }

    private void HandleDenCleared()
    {
        ready = false;
        respawnTimer = 0f;

        if (pack != null)
            pack.MinionSpawned -= OnPackMinionSpawned;

        leader = null;
        pack = null;

        enabled = false;
    }

    private void OnDenDamaged(Vector3 threatPoint)
    {
        if (!ready) return;
        if (denSite == null || denSite.IsCleared) return;

        alertLevel = Mathf.Min(encounterConfig.MaxAlertLevel, alertLevel + encounterConfig.AlertLevelPerHit);
        alertDecayDelayTimer = encounterConfig.AlertLevelDecayDelay;

        if (TryTriggerMaxAlertHowl())
            return;

        Vector3 investigatePoint = BuildDenInvestigationPoint();
        float investigateDuration = encounterConfig.DenAlertDuration + alertLevel;
        float standBonus = alertLevel * encounterConfig.InvestigateStandBonusPerAlert;

        if (leader != null)
            leader.SetInvestigationTarget(investigatePoint, investigateDuration, standBonus);

        Wolf[] occupantsSnapshot = occupants.Snapshot();
        for (int i = 0; i < occupantsSnapshot.Length; i++)
        {
            Wolf w = occupantsSnapshot[i];
            if (w == null || w == leader)
                continue;

            w.SetInvestigationTarget(investigatePoint, investigateDuration, standBonus);
        }
    }

    private bool TryTriggerMaxAlertHowl()
    {
        if (!encounterConfig.HowlAtMaxAlert)
            return false;

        if (alertLevel < encounterConfig.MaxAlertLevel)
            return false;

        if (hasTriggeredMaxAlertHowl)
            return false;

        if (leader == null)
            return false;

        if (!leader.CanHowl)
            return false;

        if (leader.pack == null)
            return false;

        if (!leader.pack.EnsureLeader(leader))
            return false;

        hasTriggeredMaxAlertHowl = true;

        leader.ClearInvestigationTarget();
        leader.SetAggroStatus(true);
        leader.StateMachine.ChangeState(leader.HowlState);

        alertLevel = Mathf.Clamp(encounterConfig.AlertLevelAfterHowl, 0f, encounterConfig.MaxAlertLevel);
        alertDecayDelayTimer = encounterConfig.AlertLevelDecayDelay;

        return true;
    }

    private Vector3 BuildDenInvestigationPoint()
    {
        if (denSite == null)
            return transform.position;

        IWorldNavigationService navigationService = encounterServices != null
            ? encounterServices.NavigationService
            : null;

        if (navigationService == null)
            return denSite.WorldPosition;

        Vector3 denCenterWorld = denSite.WorldPosition;
        Vector2Int denCenterCell = navigationService.WorldToCell(denCenterWorld);

        Transform player = FindPlayerTransform();
        Vector2Int stepDir = GetStepDirectionTowardPlayer(denCenterWorld, player);
        if (stepDir == Vector2Int.zero)
            stepDir = Vector2Int.right;

        Vector2Int currentCell = denCenterCell;

        int maxSteps = Mathf.Max(2, Mathf.CeilToInt(occupantPolicy.HomeRadius) + encounterConfig.InvestigatePointSearchRadius + 4);

        bool foundBoundaryWalkable = false;

        for (int i = 0; i < maxSteps; i++)
        {
            currentCell += stepDir;

            if (navigationService.IsWalkableCell(currentCell))
            {
                foundBoundaryWalkable = true;
                break;
            }
        }

        if (!foundBoundaryWalkable)
            return FindNearestWalkableCellAround(currentCell, encounterConfig.InvestigatePointSearchRadius, navigationService, denCenterWorld);

        int outwardSteps = Mathf.Max(
            1,
            Mathf.RoundToInt(encounterConfig.InvestigateBaseStepsFromDen + alertLevel * encounterConfig.InvestigateExtraStepsPerAlert)
        );

        for (int i = 1; i < outwardSteps; i++)
        {
            Vector2Int nextCell = currentCell + stepDir;
            if (!navigationService.IsWalkableCell(nextCell))
                break;

            currentCell = nextCell;
        }

        return navigationService.CellToWorldCenter(currentCell);
    }

    private Vector2Int GetStepDirectionTowardPlayer(Vector3 denCenterWorld, Transform player)
    {
        if (player == null)
            return Vector2Int.right;

        Vector2 dir = player.position - denCenterWorld;
        if (dir.sqrMagnitude < 0.0001f)
            return Vector2Int.right;

        dir.Normalize();

        int x = Mathf.Abs(dir.x) >= 0.35f ? (dir.x > 0f ? 1 : -1) : 0;
        int y = Mathf.Abs(dir.y) >= 0.35f ? (dir.y > 0f ? 1 : -1) : 0;

        if (x == 0 && y == 0)
        {
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                x = dir.x > 0f ? 1 : -1;
            else
                y = dir.y > 0f ? 1 : -1;
        }

        return new Vector2Int(x, y);
    }

    private Vector3 FindNearestWalkableCellAround(
        Vector2Int startCell,
        int maxTileRadius,
        IWorldNavigationService navigationService,
        Vector3 fallbackWorldPos)
    {
        if (navigationService.IsWalkableCell(startCell))
            return navigationService.CellToWorldCenter(startCell);

        for (int r = 1; r <= maxTileRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, r);
                if (navigationService.IsWalkableCell(top))
                    return navigationService.CellToWorldCenter(top);

                Vector2Int bottom = startCell + new Vector2Int(x, -r);
                if (navigationService.IsWalkableCell(bottom))
                    return navigationService.CellToWorldCenter(bottom);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(r, y);
                if (navigationService.IsWalkableCell(right))
                    return navigationService.CellToWorldCenter(right);

                Vector2Int left = startCell + new Vector2Int(-r, y);
                if (navigationService.IsWalkableCell(left))
                    return navigationService.CellToWorldCenter(left);
            }
        }

        return FindNearestWalkableSpawn(fallbackWorldPos, maxTileRadius);
    }

    private void DespawnTrackedExceptActiveChasers()
    {
        Transform player = FindPlayerTransform();

        if (pack != null)
            pack.activeMinions.Clear();

        occupants.ReleaseWhere(
            keepAlivePredicate: wolf => ShouldKeepAliveOnUnload(wolf, player),
            detachAction: DetachFromDen,
            releaseAction: wolf => wolf.RequestDespawn());

        leader = null;
        pack = null;
        respawnTimer = 0f;
        ready = false;
    }

    private Transform FindPlayerTransform()
    {
        return encounterServices != null
            ? encounterServices.PlayerLocator.GetPlayerTransform()
            : null;
    }

    private bool ShouldKeepAliveOnUnload(Wolf w, Transform player)
    {
        if (!occupantPolicy.KeepOccupantsOnUnloadIfChasingPlayer) return false;
        if (w == null) return false;
        if (player == null) return false;

        float maxDistance = occupantPolicy.KeepChaseIfWithinPlayerRange;
        float maxDistanceSqr = maxDistance * maxDistance;
        Vector3 offset = w.transform.position - player.position;
        if (offset.sqrMagnitude > maxDistanceSqr) return false;

        return IsWolfActivelyChasingPlayer(w);
    }

    private bool IsWolfActivelyChasingPlayer(Wolf w)
    {
        return w != null && w.IsChasingPlayer;
    }

    private void DetachFromDen(Wolf w)
    {
        if (w == null) return;

        var home = w.GetComponent<HomeAnchor>();
        if (home != null)
        {
            home.Center = w.transform.position;
            home.Radius = occupantPolicy.HomeRadius;
        }
    }

    private void ForceDespawnAllTracked()
    {
        if (pack != null)
            pack.activeMinions.Clear();

        occupants.ReleaseAll(wolf => wolf.RequestDespawn());
        leader = null;
        pack = null;
        respawnTimer = 0f;
        ready = false;
    }

    private void EnsureLeader()
    {
        if (!ready) return;
        if (denSite == null || !denSite.IsInitialized) return;
        if (leader != null) return;
        if (encounterConfig.LeaderPrefab == null) return;

        leader = SpawnWolf(encounterConfig.LeaderPrefab);
        if (leader == null) return;

        leader.role = WolfRole.Leader;

        pack = leader.GetComponent<PackController>();
        if (pack != null)
        {
            pack.leaderWolf = leader;
            pack.minionWolfPrefab = encounterConfig.MinionPrefab;

            pack.MinionSpawned -= OnPackMinionSpawned;
            pack.MinionSpawned += OnPackMinionSpawned;
        }
    }

    private void OnPackMinionSpawned(Wolf w)
    {
        if (w == null) return;

        occupants.Track(w, OnWolfDespawned);
    }

    private Wolf SpawnWolf(Wolf prefab)
    {
        Vector3 pos = denSite.WorldPosition + (Vector3)(Random.insideUnitCircle * occupantPolicy.SpawnRadius);
        pos = FindNearestWalkableSpawn(pos, maxTileRadius: occupantPolicy.SpawnRadius);

        if (encounterServices == null || encounterServices.EnemySpawnService == null)
            return null;

        Enemy e = encounterServices.EnemySpawnService.SpawnEnemy(prefab, pos, Quaternion.identity);

        Wolf w = e as Wolf;
        if (w == null) return null;

        var home = w.GetComponent<HomeAnchor>();
        if (home == null)
            home = w.gameObject.AddComponent<HomeAnchor>();

        home.Center = denSite.WorldPosition;
        home.Radius = occupantPolicy.HomeRadius;

        occupants.Track(w, OnWolfDespawned);

        return w;
    }

    private void OnWolfDespawned(Wolf w)
    {
        if (w == leader)
        {
            leader = null;
            pack = null;
            respawnTimer = 0f;
        }
    }

    private Vector3 FindNearestWalkableSpawn(Vector3 desiredWorldPos, int maxTileRadius = 6)
    {
        IWorldNavigationService navigationService = encounterServices != null
            ? encounterServices.NavigationService
            : null;

        if (navigationService == null)
            return desiredWorldPos;

        Vector2Int startCell = navigationService.WorldToCell(desiredWorldPos);

        if (navigationService.IsWalkableCell(startCell))
            return navigationService.CellToWorldCenter(startCell);

        for (int r = 1; r <= maxTileRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                var c1 = startCell + new Vector2Int(x, r);
                if (navigationService.IsWalkableCell(c1)) return navigationService.CellToWorldCenter(c1);

                var c2 = startCell + new Vector2Int(x, -r);
                if (navigationService.IsWalkableCell(c2)) return navigationService.CellToWorldCenter(c2);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                var c3 = startCell + new Vector2Int(r, y);
                if (navigationService.IsWalkableCell(c3)) return navigationService.CellToWorldCenter(c3);

                var c4 = startCell + new Vector2Int(-r, y);
                if (navigationService.IsWalkableCell(c4)) return navigationService.CellToWorldCenter(c4);
            }
        }

        return desiredWorldPos;
    }

    private bool HasEncounterConfig()
    {
        return encounterConfig != null && occupantPolicy != null;
    }

    private IWorldEncounterSite ResolveDenSite()
    {
        if (encounterSiteComponent is IWorldEncounterSite assignedEncounterSite)
            return assignedEncounterSite;

        encounterSiteComponent = FindEncounterSiteComponent();
        if (encounterSiteComponent is IWorldEncounterSite discoveredEncounterSite)
            return discoveredEncounterSite;

        return null;
    }

    private MonoBehaviour FindEncounterSiteComponent()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IWorldEncounterSite)
                return behaviours[i];
        }

        return null;
    }
}
