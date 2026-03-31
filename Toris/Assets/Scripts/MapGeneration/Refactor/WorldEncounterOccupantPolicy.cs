using System;
using UnityEngine;

[Serializable]
public sealed class WorldEncounterOccupantPolicy
{
    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 6f;

    [Header("Spawn Area")]
    [SerializeField] private int spawnRadius = 4;

    [Header("Chunk Unload Behavior")]
    [SerializeField] private bool keepOccupantsOnUnloadIfChasingPlayer = true;
    [SerializeField] private float keepChaseIfWithinPlayerRange = 40f;

    [Header("Home")]
    [SerializeField] private float homeRadius = 8f;

    public float RespawnDelay => respawnDelay;
    public int SpawnRadius => spawnRadius;
    public bool KeepOccupantsOnUnloadIfChasingPlayer => keepOccupantsOnUnloadIfChasingPlayer;
    public float KeepChaseIfWithinPlayerRange => keepChaseIfWithinPlayerRange;
    public float HomeRadius => homeRadius;

    public void ApplyLegacyValues(
        float legacyRespawnDelay,
        int legacySpawnRadius,
        bool legacyKeepOccupantsOnUnloadIfChasingPlayer,
        float legacyKeepChaseIfWithinPlayerRange,
        float legacyHomeRadius)
    {
        respawnDelay = legacyRespawnDelay;
        spawnRadius = legacySpawnRadius;
        keepOccupantsOnUnloadIfChasingPlayer = legacyKeepOccupantsOnUnloadIfChasingPlayer;
        keepChaseIfWithinPlayerRange = legacyKeepChaseIfWithinPlayerRange;
        homeRadius = legacyHomeRadius;
    }

#if UNITY_EDITOR
    public void Validate()
    {
        respawnDelay = Mathf.Max(0f, respawnDelay);
        spawnRadius = Mathf.Max(0, spawnRadius);
        keepChaseIfWithinPlayerRange = Mathf.Max(0f, keepChaseIfWithinPlayerRange);
        homeRadius = Mathf.Max(0f, homeRadius);
    }
#endif
}
