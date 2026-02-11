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
    [SerializeField] private float spawnRadius = 2.5f;

    private WolfDen den;

    private bool ready;
    private Wolf leader;
    private PackController pack;
    private readonly List<Wolf> tracked = new();
    private float respawnTimer;

    private void Awake()
    {
        den = GetComponent<WolfDen>();
    }

    private void OnEnable()
    {
        ResetRuntime();
    }

    // --- IPoolable ---
    public void OnSpawned()
    {
        // When den comes out of pool, clear old refs
        ResetRuntime();
    }

    public void OnDespawned()
    {
        // When den goes back to pool (chunk unload), hard-clean ALL wolves spawned by this den
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

        if (leader != null) return;

        respawnTimer += Time.deltaTime;
        if (respawnTimer >= leaderRespawnDelay)
        {
            respawnTimer = 0f;
            EnsureLeader();
        }
    }

    public void OnDenCleared()
    {
        enabled = false;
        ForceDespawnAllTracked();
        ResetRuntime();
    }

    private void ForceDespawnAllTracked()
    {
        if (pack != null)
        {
            var minions = pack.activeMinions.ToArray();
            pack.activeMinions.Clear();

            for (int i = 0; i < minions.Length; i++)
                if (minions[i] != null)
                    minions[i].RequestDespawn();
        }
        var trackedCopy = tracked.ToArray();
        tracked.Clear();

        for (int i = 0; i < trackedCopy.Length; i++)
            if (trackedCopy[i] != null)
                trackedCopy[i].RequestDespawn();

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

        Enemy e = GameplayPoolManager.Instance != null
            ? GameplayPoolManager.Instance.SpawnEnemy(prefab, pos, Quaternion.identity)
            : Instantiate(prefab, pos, Quaternion.identity);

        Wolf w = e as Wolf;
        if (w == null) return null;

        var home = w.GetComponent<HomeAnchor>();
        if (home == null)
            home = w.gameObject.AddComponent<HomeAnchor>();

        home.center = den.WorldPosition;
        home.radius = 8f;

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
}
