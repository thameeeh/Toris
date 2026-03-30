using UnityEngine;

public sealed class EnemySpawnService : IEnemySpawnService
{
    private readonly GameplayPoolManager gameplayPoolManager;

    public EnemySpawnService(GameplayPoolManager gameplayPoolManager)
    {
        this.gameplayPoolManager = gameplayPoolManager;
    }

    public Enemy SpawnEnemy(Enemy prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            return null;

        if (gameplayPoolManager == null)
        {
            Debug.LogWarning($"{nameof(EnemySpawnService)} has no {nameof(GameplayPoolManager)}.");
            return null;
        }

        return gameplayPoolManager.SpawnEnemy(prefab, position, rotation);
    }
}