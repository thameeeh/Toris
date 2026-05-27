using UnityEngine;

[CreateAssetMenu(
    menuName = "Outland Haven/WorldGen/Wildlife/Wildlife Spawn Definition",
    fileName = "WildlifeSpawnDefinition")]
public sealed class WorldWildlifeSpawnDefinition : ScriptableObject
{
    [SerializeField] private Enemy enemyPrefab;

    public Enemy EnemyPrefab => enemyPrefab;
    public bool IsValid => enemyPrefab != null;
}
