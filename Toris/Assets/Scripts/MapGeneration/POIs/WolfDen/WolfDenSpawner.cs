using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(WolfDen))]
public sealed class WolfDenSpawner : MonoBehaviour, IPoolable
{
    [Header("Prefabs")]
    [SerializeField] private Wolf leaderPrefab;
    [SerializeField] private Wolf minionPrefab;

    [Header("Leader Respawn")]
    [SerializeField] private float leaderRespawnDelay = 6f;

    [Header("Spawn Area")]
    [SerializeField] private int spawnRadius;

    [Header("Chunk Unload Behavior")]
    [SerializeField] private bool keepChasingWolvesOnUnload = true;
    [SerializeField] private float keepChaseIfWithinPlayerRange = 40f;

    [Header("Home")]
    [SerializeField] private float homeRadius = 8f;

    [Header("Den Alert")]
    [SerializeField] private float denAlertDuration = 4f;
    [SerializeField] private float alertLevelDecayDelay = 2.5f;
    [SerializeField] private float alertLevelDecayRate = 0.35f;
    [SerializeField] private float alertLevelPerHit = 1f;
    [SerializeField] private float maxAlertLevel = 4f;

    [Header("Den Alert Escalation")]
    [SerializeField] private float investigateStandBonusPerAlert = 0.35f;
    [SerializeField] private float investigateBaseStepsFromDen = 1f;
    [SerializeField] private float investigateExtraStepsPerAlert = 1f;
    [SerializeField] private int investigatePointSearchRadius = 6;

    [Header("Max Alert Response")]
    [SerializeField] private bool howlAtMaxAlert = true;
    [SerializeField] private float alertLevelAfterHowl = 0f;

    private WolfDen den;

    private bool ready;
    private Wolf leader;
    private PackController pack;
    private readonly List<Wolf> tracked = new();
    private float respawnTimer;

    private float alertLevel;
    private float alertDecayDelayTimer;
    private bool hasTriggeredMaxAlertHowl;

    private void Awake()
    {
        den = GetComponent<WolfDen>();
    }

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

        if (keepChasingWolvesOnUnload)
            DespawnTrackedExceptActiveChasers();
        else
            ForceDespawnAllTracked();

        ResetRuntime();
    }

    private void ResetRuntime()
    {
        ready = false;
        tracked.Clear();
        leader = null;
        pack = null;
        respawnTimer = 0f;

        alertLevel = 0f;
        alertDecayDelayTimer = 0f;
        hasTriggeredMaxAlertHowl = false;
    }

    private void SubscribeToDen()
    {
        if (den == null)
            den = GetComponent<WolfDen>();

        if (den != null)
        {
            den.DamagedAlert -= OnDenDamaged;
            den.DamagedAlert += OnDenDamaged;
        }
    }

    private void UnsubscribeFromDen()
    {
        if (den != null)
            den.DamagedAlert -= OnDenDamaged;
    }

    public void OnDenInitialized()
    {
        if (den == null)
            den = GetComponent<WolfDen>();

        ready = true;

        if (den != null && !den.IsCleared)
            EnsureLeader();
    }

    private void Update()
    {
        if (!ready) return;
        if (den == null) return;
        if (den.IsCleared) return;

        tracked.RemoveAll(w => w == null);

        if (leader == null)
        {
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= leaderRespawnDelay)
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
            alertLevel = Mathf.Max(0f, alertLevel - alertLevelDecayRate * Time.deltaTime);
        }

        if (alertLevel < maxAlertLevel)
        {
            hasTriggeredMaxAlertHowl = false;
        }
    }

    public void OnDenCleared()
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
        if (den == null || den.IsCleared) return;

        alertLevel = Mathf.Min(maxAlertLevel, alertLevel + alertLevelPerHit);
        alertDecayDelayTimer = alertLevelDecayDelay;

        if (TryTriggerMaxAlertHowl())
            return;

        Vector3 investigatePoint = BuildDenInvestigationPoint();
        float investigateDuration = denAlertDuration + alertLevel;
        float standBonus = alertLevel * investigateStandBonusPerAlert;

        if (leader != null)
            leader.SetInvestigationTarget(investigatePoint, investigateDuration, standBonus);

        for (int i = 0; i < tracked.Count; i++)
        {
            Wolf w = tracked[i];
            if (w == null || w == leader)
                continue;

            w.SetInvestigationTarget(investigatePoint, investigateDuration, standBonus);
        }
    }

    private bool TryTriggerMaxAlertHowl()
    {
        if (!howlAtMaxAlert)
            return false;

        if (alertLevel < maxAlertLevel)
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

        alertLevel = Mathf.Clamp(alertLevelAfterHowl, 0f, maxAlertLevel);
        alertDecayDelayTimer = alertLevelDecayDelay;

        return true;
    }

    private Vector3 BuildDenInvestigationPoint()
    {
        if (den == null)
            return transform.position;

        var nav = TileNavWorld.Instance;
        if (nav == null)
            return den.WorldPosition;

        Vector3 denCenterWorld = den.WorldPosition;
        Vector2Int denCenterCell = nav.WorldToCell(denCenterWorld);

        Transform player = FindPlayerTransform();
        Vector2Int stepDir = GetStepDirectionTowardPlayer(denCenterWorld, player);
        if (stepDir == Vector2Int.zero)
            stepDir = Vector2Int.right;

        Vector2Int currentCell = denCenterCell;

        int maxSteps = Mathf.Max(2, Mathf.CeilToInt(homeRadius) + investigatePointSearchRadius + 4);

        bool foundBoundaryWalkable = false;

        for (int i = 0; i < maxSteps; i++)
        {
            currentCell += stepDir;

            if (nav.IsWalkableCell(currentCell))
            {
                foundBoundaryWalkable = true;
                break;
            }
        }

        if (!foundBoundaryWalkable)
            return FindNearestWalkableCellAround(currentCell, investigatePointSearchRadius, nav, denCenterWorld);

        int outwardSteps = Mathf.Max(
            1,
            Mathf.RoundToInt(investigateBaseStepsFromDen + alertLevel * investigateExtraStepsPerAlert)
        );

        for (int i = 1; i < outwardSteps; i++)
        {
            Vector2Int nextCell = currentCell + stepDir;
            if (!nav.IsWalkableCell(nextCell))
                break;

            currentCell = nextCell;
        }

        return nav.CellToWorldCenter(currentCell);
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

    private Vector3 FindNearestWalkableCellAround(Vector2Int startCell, int maxTileRadius, TileNavWorld nav, Vector3 fallbackWorldPos)
    {
        if (nav.IsWalkableCell(startCell))
            return nav.CellToWorldCenter(startCell);

        for (int r = 1; r <= maxTileRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                Vector2Int top = startCell + new Vector2Int(x, r);
                if (nav.IsWalkableCell(top))
                    return nav.CellToWorldCenter(top);

                Vector2Int bottom = startCell + new Vector2Int(x, -r);
                if (nav.IsWalkableCell(bottom))
                    return nav.CellToWorldCenter(bottom);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                Vector2Int right = startCell + new Vector2Int(r, y);
                if (nav.IsWalkableCell(right))
                    return nav.CellToWorldCenter(right);

                Vector2Int left = startCell + new Vector2Int(-r, y);
                if (nav.IsWalkableCell(left))
                    return nav.CellToWorldCenter(left);
            }
        }

        return FindNearestWalkableSpawn(fallbackWorldPos, maxTileRadius);
    }

    private void DespawnTrackedExceptActiveChasers()
    {
        Transform player = FindPlayerTransform();

        if (pack != null)
        {
            var minions = pack.activeMinions.ToArray();
            pack.activeMinions.Clear();

            for (int i = 0; i < minions.Length; i++)
            {
                Wolf w = minions[i];
                if (w == null) continue;

                if (ShouldKeepAliveOnUnload(w, player))
                {
                    DetachFromDen(w);
                    continue;
                }

                w.RequestDespawn();
            }
        }

        var trackedCopy = tracked.ToArray();
        tracked.Clear();

        for (int i = 0; i < trackedCopy.Length; i++)
        {
            Wolf w = trackedCopy[i];
            if (w == null) continue;

            if (ShouldKeepAliveOnUnload(w, player))
            {
                DetachFromDen(w);
                continue;
            }

            w.RequestDespawn();
        }

        leader = null;
        pack = null;
        respawnTimer = 0f;
        ready = false;
    }

    private Transform FindPlayerTransform()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        return p != null ? p.transform : null;
    }

    private bool ShouldKeepAliveOnUnload(Wolf w, Transform player)
    {
        if (!keepChasingWolvesOnUnload) return false;
        if (w == null) return false;
        if (player == null) return false;

        float d = Vector3.Distance(w.transform.position, player.position);
        if (d > keepChaseIfWithinPlayerRange) return false;

        return IsWolfActivelyChasingPlayer(w);
    }

    private bool IsWolfActivelyChasingPlayer(Wolf w)
    {
        return w != null && w.IsChasingPlayer;
    }

    private void DetachFromDen(Wolf w)
    {
        if (w == null) return;

        w.Despawned -= OnWolfDespawned;

        var home = w.GetComponent<HomeAnchor>();
        if (home != null)
        {
            home.Center = w.transform.position;
            home.Radius = homeRadius;
        }
    }

    private void ForceDespawnAllTracked()
    {
        if (pack != null)
        {
            var minions = pack.activeMinions.ToArray();
            pack.activeMinions.Clear();

            for (int i = 0; i < minions.Length; i++)
            {
                if (minions[i] != null)
                    minions[i].RequestDespawn();
            }
        }

        var trackedCopy = tracked.ToArray();
        tracked.Clear();

        for (int i = 0; i < trackedCopy.Length; i++)
        {
            if (trackedCopy[i] != null)
                trackedCopy[i].RequestDespawn();
        }

        tracked.Clear();
        leader = null;
        pack = null;
        respawnTimer = 0f;
        ready = false;
    }

    private void EnsureLeader()
    {
        if (!ready) return;
        if (den == null || !den.IsInitialized) return;
        if (leader != null) return;
        if (leaderPrefab == null) return;

        leader = SpawnWolf(leaderPrefab);
        if (leader == null) return;

        leader.role = WolfRole.Leader;

        pack = leader.GetComponent<PackController>();
        if (pack != null)
        {
            pack.leaderWolf = leader;
            pack.minionWolfPrefab = minionPrefab;

            pack.MinionSpawned -= OnPackMinionSpawned;
            pack.MinionSpawned += OnPackMinionSpawned;
        }
    }

    private void OnPackMinionSpawned(Wolf w)
    {
        if (w == null) return;

        if (!tracked.Contains(w))
        {
            tracked.Add(w);
            w.Despawned += OnWolfDespawned;
        }
    }

    private Wolf SpawnWolf(Wolf prefab)
    {
        Vector3 pos = den.WorldPosition + (Vector3)(Random.insideUnitCircle * spawnRadius);
        pos = FindNearestWalkableSpawn(pos, maxTileRadius: spawnRadius);

        Enemy e = GameplayPoolManager.Instance != null
            ? GameplayPoolManager.Instance.SpawnEnemy(prefab, pos, Quaternion.identity)
            : Instantiate(prefab, pos, Quaternion.identity);

        Wolf w = e as Wolf;
        if (w == null) return null;

        var home = w.GetComponent<HomeAnchor>();
        if (home == null)
            home = w.gameObject.AddComponent<HomeAnchor>();

        home.Center = den.WorldPosition;
        home.Radius = homeRadius;

        if (!tracked.Contains(w))
        {
            tracked.Add(w);
            w.Despawned += OnWolfDespawned;
        }

        return w;
    }

    private void OnWolfDespawned(Enemy e)
    {
        var w = e as Wolf;
        if (w != null)
            tracked.Remove(w);

        if (w == leader)
        {
            leader = null;
            pack = null;
            respawnTimer = 0f;
        }
    }

    private Vector3 FindNearestWalkableSpawn(Vector3 desiredWorldPos, int maxTileRadius = 6)
    {
        var nav = TileNavWorld.Instance;
        if (nav == null) return desiredWorldPos;

        Vector2Int startCell = nav.WorldToCell(desiredWorldPos);

        if (nav.IsWalkableCell(startCell))
            return nav.CellToWorldCenter(startCell);

        for (int r = 1; r <= maxTileRadius; r++)
        {
            for (int x = -r; x <= r; x++)
            {
                var c1 = startCell + new Vector2Int(x, r);
                if (nav.IsWalkableCell(c1)) return nav.CellToWorldCenter(c1);

                var c2 = startCell + new Vector2Int(x, -r);
                if (nav.IsWalkableCell(c2)) return nav.CellToWorldCenter(c2);
            }

            for (int y = -r + 1; y <= r - 1; y++)
            {
                var c3 = startCell + new Vector2Int(r, y);
                if (nav.IsWalkableCell(c3)) return nav.CellToWorldCenter(c3);

                var c4 = startCell + new Vector2Int(-r, y);
                if (nav.IsWalkableCell(c4)) return nav.CellToWorldCenter(c4);
            }
        }

        return desiredWorldPos;
    }
}