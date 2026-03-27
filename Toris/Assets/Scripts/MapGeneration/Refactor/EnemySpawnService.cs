using UnityEngine;

public sealed class EnemySpawnService : IEnemySpawnService
{
    public Enemy SpawnEnemy(Enemy prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (GameplayPoolManager.Instance != null)
            return GameplayPoolManager.Instance.SpawnEnemy(prefab, position, rotation);

        return Object.Instantiate(prefab, position, rotation);
    }
}