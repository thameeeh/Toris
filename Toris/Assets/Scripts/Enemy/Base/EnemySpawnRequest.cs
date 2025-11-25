using UnityEngine;

[System.Serializable]
public struct EnemySpawnRequest
{
    public Enemy Prefab;
    public Vector3 Position;
    public Quaternion Rotation;
    public Transform Parent;
    public Transform SpawnPoint;
    public string FactionId;
    public int DifficultyTier;
    public EnemyLoadout Loadout;

    public EnemySpawnRequest(Enemy prefab, Vector3 position, Quaternion rotation)
    {
        Prefab = prefab;
        Position = position;
        Rotation = rotation;
        Parent = null;
        SpawnPoint = null;
        FactionId = string.Empty;
        DifficultyTier = 0;
        Loadout = null;
    }

    public EnemySpawnRequest WithParent(Transform parent)
    {
        Parent = parent;
        return this;
    }

    public EnemySpawnRequest WithSpawnPoint(Transform spawnPoint)
    {
        SpawnPoint = spawnPoint;
        Position = spawnPoint ? spawnPoint.position : Position;
        Rotation = spawnPoint ? spawnPoint.rotation : Rotation;
        return this;
    }

    public EnemySpawnRequest WithMeta(string factionId, int difficulty)
    {
        FactionId = factionId;
        DifficultyTier = difficulty;
        return this;
    }

    public EnemySpawnRequest WithLoadout(EnemyLoadout loadout)
    {
        Loadout = loadout;
        return this;
    }
}