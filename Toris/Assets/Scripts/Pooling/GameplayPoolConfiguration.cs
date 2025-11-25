using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Gameplay/Pools/Gameplay Pool Configuration", fileName = "GameplayPoolConfiguration")]
public class GameplayPoolConfiguration : ScriptableObject
{
    [SerializeField]
    private ProjectilePoolSettings[] projectilePools = Array.Empty<ProjectilePoolSettings>();

    [SerializeField]
    private EnemyPoolSettings[] enemyPools = Array.Empty<EnemyPoolSettings>();

    public ProjectilePoolSettings[] ProjectilePools => projectilePools;
    public EnemyPoolSettings[] EnemyPools => enemyPools;
}

[Serializable]
public sealed class ProjectilePoolSettings
{
    public Projectile prefab;
    [Min(0)] public int prewarmCount = 8;
    [Min(1)] public int defaultCapacity = 32;
    [Min(1)] public int maxSize = 256;
    [Tooltip("Optional parent transform for pooled projectiles.")]
    public Transform parentOverride;
}

[Serializable]
public sealed class EnemyPoolSettings
{
    public Enemy prefab;
    [Min(0)] public int prewarmCount = 4;
    [Min(1)] public int defaultCapacity = 8;
    [Min(1)] public int maxSize = 64;
    [Tooltip("Optional parent transform for pooled enemies.")]
    public Transform parentOverride;
}