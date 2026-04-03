using UnityEngine;

public interface IEnemySpawnService
{
    Enemy SpawnEnemy(Enemy prefab, Vector3 position, Quaternion rotation);
}