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

    [Header("World / Chunk")]
    [SerializeField] private MapGenerator _mapGenerator; // NEW: to register minions in chunks

    private void Awake()
    {
        // Resolve MapGenerator so we can register minions into its chunk map
        if (_mapGenerator == null)
        {
            _mapGenerator = FindFirstObjectByType<MapGenerator>();
        }

        if (leaderWolf == null)
        {
            leaderWolf = GetComponent<Wolf>();
        }

        if (leaderWolf != null)
        {
            RegisterLeader(leaderWolf);
        }
    }

    public bool CanLeaderHowl(Wolf requester = null)
    {
        Wolf leader = ResolveLeader(requester);
        if (leader == null) return false;
        if (!leader.CanHowl) return false;
        if (Time.time < _lastHowlTimestamp + howlCooldownDuration) return false;
        if (GetActiveMinionCount() >= maxPackSize) return false;
        return true;
    }

    public void HandleLeaderHowl(Wolf howlingWolf = null)
    {
        if (!CanLeaderHowl(howlingWolf)) return;

        _lastHowlTimestamp = Time.time;
        SpawnMinions(minionsPerHowl, leaderWolf.transform.position);
    }

    public int GetActiveMinionCount()
    {
        // Clean out destroyed entries
        activeMinions.RemoveAll(m => m == null);
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
            Vector2 spawnPoint = GetRandomSpawnPoint(spawnCenter);

            Wolf newMinion;

            if (poolManager != null)
            {
                Enemy enemy = poolManager.SpawnEnemy(minionWolfPrefab, spawnPoint, Quaternion.identity);
                newMinion = enemy as Wolf;

                if (newMinion == null)
                {
                    //Debug.LogError("Minion prefab is not a Wolf or pooled enemy is not a Wolf.");
                    continue;
                }
            }
            else
            {
                newMinion = Instantiate(minionWolfPrefab, spawnPoint, Quaternion.identity);
            }

            newMinion.role = WolfRole.Minion;
            newMinion.pack = this;

            newMinion.Despawned += OnMinionDespawned;

            activeMinions.Add(newMinion);

            if (_mapGenerator == null)
            {
                _mapGenerator = FindFirstObjectByType<MapGenerator>();
            }

            if (_mapGenerator != null)
            {
                _mapGenerator.RegisterEnemyInChunk(newMinion, newMinion.transform.position);
            }
        }
    }

    private Vector2 GetRandomSpawnPoint(Vector2 spawnCenter)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            if (direction == Vector2.zero)
            {
                continue;
            }

            Vector2 candidatePos = spawnCenter + direction * spawnDistance;
            Collider2D hit = Physics2D.OverlapCircle(candidatePos, 0.3f,
                LayerMask.GetMask("Default", "Ground", "Walls"));
            if (hit == null) return candidatePos;
        }
        return spawnCenter + Random.insideUnitCircle * spawnDistance * 0.5f;
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

    private void RegisterLeader(Wolf leader)
    {
        if (leader == null) return;

        if (leaderWolf != null && leaderWolf != leader)
        {
            leaderWolf.role = WolfRole.Minion;
            leaderWolf.pack = null;
        }

        leaderWolf = leader;
        leaderWolf.role = WolfRole.Leader;
        leaderWolf.pack = this;
    }

    private Wolf ResolveLeader(Wolf requester)
    {
        if (leaderWolf == null) return null;
        if (requester != null && requester != leaderWolf) return null;

        return leaderWolf;
    }

    public void DespawnAllMinions()
    {
        foreach (var minion in activeMinions)
        {
            if (minion == null) continue;

            minion.Despawned -= OnMinionDespawned;
            minion.RequestDespawn();
        }

        activeMinions.Clear();
    }

    public void NotifyMinionDespawned(Wolf minion)
    {
        if (minion == null) return;

        minion.Despawned -= OnMinionDespawned;
        activeMinions.Remove(minion);
    }

    private void OnMinionDespawned(Enemy enemy)
    {
        var wolf = enemy as Wolf;
        if (wolf == null) return;

        activeMinions.Remove(wolf);
    }
}
