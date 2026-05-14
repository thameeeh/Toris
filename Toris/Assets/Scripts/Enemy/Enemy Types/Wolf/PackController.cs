using System.Collections.Generic;
using UnityEngine;

public class PackController : MonoBehaviour
{
    [Header("Pack Settings")]
    public Wolf leaderWolf;
    public Wolf minionWolfPrefab;
    public int maxPackSize = 4;
    public int minionsPerHowl = 2;
    public float spawnDistance = 2.5f;
    public float howlCooldownDuration = 12f;

    [Header("Runtime")]
    public List<Wolf> activeMinions = new List<Wolf>();
    private float _lastHowlTimestamp = -999f;

    public System.Action<Wolf> MinionSpawned;

    private void Awake()
    {
        if (leaderWolf == null)
            leaderWolf = GetComponent<Wolf>();

        if (leaderWolf != null)
            RegisterLeader(leaderWolf);
    }

    public bool EnsureLeader(Wolf candidate)
    {
        if (candidate == null) return false;

        if (leaderWolf == null)
        {
            RegisterLeader(candidate);
            return true;
        }

        return leaderWolf == candidate;
    }

    public bool CanLeaderHowl(Wolf requester = null)
    {
        Wolf leader = ResolveLeader(requester);
        if (leader == null) return false;
        if (!IsWolfAliveAndActive(leader)) return false;
        if (!leader.CanHowl) return false;
        if (Time.time < _lastHowlTimestamp + howlCooldownDuration) return false;
        if (GetActiveMinionCount() >= maxPackSize) return false;
        return true;
    }

    public bool TryReserveLeaderHowl(Wolf requester = null)
    {
        if (!CanLeaderHowl(requester))
            return false;

        _lastHowlTimestamp = Time.time;
        return true;
    }

    public void HandleLeaderHowl(Wolf howlingWolf = null)
    {
        Wolf leader = ResolveLeader(howlingWolf);
        if (leader == null) return;
        if (!IsWolfAliveAndActive(leader)) return;

        SpawnMinions(minionsPerHowl, leader.transform.position);
    }

    public int GetActiveMinionCount()
    {
        activeMinions.RemoveAll(m => !IsWolfAliveAndActive(m));
        return activeMinions.Count;
    }

    public void SpawnMinions(int requestedCount, Vector2 spawnCenter)
    {
        if (minionWolfPrefab == null) return;

        int availableSlots = Mathf.Max(0, maxPackSize - GetActiveMinionCount());
        int spawnAmount = Mathf.Min(requestedCount, availableSlots);

        var poolManager = GameplayPoolManager.Instance;

        for (int i = 0; i < spawnAmount; i++)
        {
            if (!TryGetRandomSpawnPoint(spawnCenter, out Vector2 spawnPoint))
                continue;

            Wolf newMinion;

            if (poolManager != null)
            {
                Enemy enemy = poolManager.SpawnEnemy(minionWolfPrefab, spawnPoint, Quaternion.identity);
                newMinion = enemy as Wolf;
                if (newMinion == null) continue;
            }
            else
            {
                newMinion = Instantiate(minionWolfPrefab, spawnPoint, Quaternion.identity);
            }

            newMinion.role = WolfRole.Minion;
            newMinion.pack = this;

            CopyLeaderHomeToMinion(newMinion);

            newMinion.Died -= OnMinionReleased;
            newMinion.Died += OnMinionReleased;
            newMinion.Despawned -= OnMinionReleased;
            newMinion.Despawned += OnMinionReleased;

            activeMinions.Add(newMinion);

            MinionSpawned?.Invoke(newMinion);
        }
    }

    private bool TryGetRandomSpawnPoint(Vector2 spawnCenter, out Vector2 spawnPoint)
    {
        const int maxSpawnAttempts = 8;
        const float spawnCheckRadius = 0.3f;

        TileNavWorld nav = TileNavWorld.Instance;
        if (nav != null)
        {
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                if (direction == Vector2.zero) continue;

                Vector2 candidatePos = spawnCenter + direction * spawnDistance;
                if (nav.TryFindNearestWalkableWorldPosition(candidatePos, 0, out Vector3 resolvedPosition))
                {
                    spawnPoint = resolvedPosition;
                    return true;
                }
            }

            int nearestSearchRadius = Mathf.CeilToInt(spawnDistance) + 2;
            if (nav.TryFindNearestWalkableWorldPosition(spawnCenter, nearestSearchRadius, out Vector3 nearestPosition))
            {
                spawnPoint = nearestPosition;
                return true;
            }

            spawnPoint = spawnCenter;
            return false;
        }

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction == Vector2.zero) continue;

            Vector2 candidatePos = spawnCenter + direction * spawnDistance;
            Collider2D hit = Physics2D.OverlapCircle(candidatePos, spawnCheckRadius,
                LayerMask.GetMask("Default", "Ground", "Walls"));

            if (hit == null)
            {
                spawnPoint = candidatePos;
                return true;
            }
        }

        spawnPoint = spawnCenter + Random.insideUnitCircle * spawnDistance * 0.5f;
        return true;
    }

    private void RegisterLeader(Wolf leader)
    {
        leaderWolf = leader;

        leaderWolf.Despawned -= OnLeaderDespawned;
        leaderWolf.Despawned += OnLeaderDespawned;
    }

    private void OnLeaderDespawned(Enemy e)
    {
        DespawnAllMinions();
    }

    private Wolf ResolveLeader(Wolf requester)
    {
        if (leaderWolf == null) return null;
        if (requester != null && requester != leaderWolf) return null;
        return leaderWolf;
    }

    private void DespawnAllMinions()
    {
        var copy = activeMinions.ToArray();
        activeMinions.Clear();

        for (int i = 0; i < copy.Length; i++)
            if (copy[i] != null)
                copy[i].RequestDespawn();
    }

    public void NotifyMinionDespawned(Wolf minion)
    {
        if (minion == null) return;
        activeMinions.Remove(minion);
    }

    private void OnMinionDespawned(Enemy enemy)
    {
        var wolf = enemy as Wolf;
        if (wolf == null) return;
        activeMinions.Remove(wolf);
    }

    private void OnMinionReleased(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemy.Died -= OnMinionReleased;
        enemy.Despawned -= OnMinionReleased;
        OnMinionDespawned(enemy);
    }

    private static bool IsWolfAliveAndActive(Wolf wolf)
    {
        return wolf != null
               && wolf.gameObject.activeInHierarchy
               && wolf.CurrentHealth > 0f;
    }

    private void CopyLeaderHomeToMinion(Wolf newMinion)
    {
        if (newMinion == null)
            return;

        if (leaderWolf == null)
            return;

        if (!leaderWolf.TryGetComponent<HomeAnchor>(out var leaderHome) || leaderHome == null)
            return;

        if (!newMinion.TryGetComponent<HomeAnchor>(out var minionHome) || minionHome == null)
        {
            minionHome = newMinion.gameObject.AddComponent<HomeAnchor>();
        }

        minionHome.Center = leaderHome.Center;
        minionHome.Radius = leaderHome.Radius;

        newMinion.RefreshHomeAnchor();
    }
}

