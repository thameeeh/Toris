using UnityEngine;

public interface IDirectHitDamageable : IDamageable
{
    void ApplyDirectHit(float damageAmount, Vector2 worldHitPosition);
}
