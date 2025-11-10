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

    public bool CanLeaderHowl()
    {
        if (leaderWolf == null || !leaderWolf.CanHowl) return false;
        if (Time.time < _lastHowlTimestamp + howlCooldownDuration) return false;
        if (GetActiveMinionCount() >= maxPackSize) return false;
        return true;
    }

    public void HandleLeaderHowl()
    {
        _lastHowlTimestamp = Time.time;
        SpawnMinions(minionsPerHowl);
    }

    public int GetActiveMinionCount()
    {
        activeMinions.RemoveAll(m => m == null);
        return activeMinions.Count;
    }

    public void SpawnMinions(int requestedCount)
    {
        if (minionWolfPrefab == null) return;

        int availableSlots = Mathf.Max(0, maxPackSize - GetActiveMinionCount());
        int spawnAmount = Mathf.Min(requestedCount, availableSlots);

        for (int i = 0; i < spawnAmount; i++)
        {
            Vector2 spawnPoint = GetRandomSpawnPoint();
            Wolf newMinion = Instantiate(minionWolfPrefab, spawnPoint, Quaternion.identity);
            newMinion.role = WolfRole.Minion;
            newMinion.healthMultiplier = 1f;

            if (newMinion.EnemyHowlBaseInstance != null)
                newMinion.EnemyHowlBaseInstance = null;

            activeMinions.Add(newMinion);
        }
    }

    private Vector2 GetRandomSpawnPoint()
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2 direction = Random.insideUnitCircle.normalized;
            Vector2 candidatePos = (Vector2)leaderWolf.transform.position + direction * spawnDistance;
            Collider2D hit = Physics2D.OverlapCircle(candidatePos, 0.3f, LayerMask.GetMask("Default", "Ground", "Walls"));
            if (hit == null) return candidatePos;
        }
        return (Vector2)leaderWolf.transform.position + Random.insideUnitCircle * spawnDistance * 0.5f;
    }

    public void DespawnAllMinions()
    {
        foreach (var minion in activeMinions)
            if (minion != null) Destroy(minion.gameObject);
        activeMinions.Clear();
    }
}
