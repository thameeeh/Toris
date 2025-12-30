using UnityEngine;

public readonly struct EnemySfxContext
{
    public readonly EnemySfx Hub;
    public readonly Transform Transform;
    public readonly Enemy Enemy;

    public EnemySfxContext(
        EnemySfx hub,
        Transform transform,
        Enemy enemy)
    {
        Hub = hub;
        Transform = transform;
        Enemy = enemy;
    }

    public bool HasAudio => AudioBootstrap.Sfx != null;
}
