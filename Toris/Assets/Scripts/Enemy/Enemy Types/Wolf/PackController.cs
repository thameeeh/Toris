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

    private void Awake()
    {
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
        activeMinions.RemoveAll(m => m == null);
        return activeMinions.Count;
    }

    public void SpawnMinions(int requestedCount, Vector2 spawnCenter)
    {
        if (minionWolfPrefab == null) return;

        int availableSlots = Mathf.Max(0, maxPackSize - GetActiveMinionCount());
        int spawnAmount = Mathf.Min(requestedCount, availableSlots);

        for (int i = 0; i < spawnAmount; i++)
        {
            Vector2 spawnPoint = GetRandomSpawnPoint(spawnCenter);
            Wolf newMinion = Instantiate(minionWolfPrefab, spawnPoint, Quaternion.identity);
            newMinion.role = WolfRole.Minion;

            newMinion.pack = this;

            activeMinions.Add(newMinion);
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
            Collider2D hit = Physics2D.OverlapCircle(candidatePos, 0.3f, LayerMask.GetMask("Default", "Ground", "Walls"));
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
        if (leader== null) return;

        if (leaderWolf != null && leaderWolf != leader)
        {
            leaderWolf.role = WolfRole.Minion;
            leaderWolf.pack = null;
        }

        leaderWolf = leader;
        leaderWolf.role = WolfRole.Leader;
        leaderWolf.pack = this;
    }

    private  Wolf ResolveLeader(Wolf requester)
    {
        if (leaderWolf == null) return null;
        if (requester != null && requester != leaderWolf) return null;

        return leaderWolf;
    }
    public void DespawnAllMinions()
    {
        foreach (var minion in activeMinions)
            if (minion != null) Destroy(minion.gameObject);
        activeMinions.Clear();
    }
    private void OnDisable()
    {
        DespawnAllMinions();
    }
}
