using UnityEngine;

public interface IEnemyAggroTarget
{
    Transform TargetTransform { get; }
    Vector2 TargetPosition { get; }
    bool IsTargetable { get; }

    void ReceiveEnemyHit(float amount, HitData hitData);
}