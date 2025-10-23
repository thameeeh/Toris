using System;
using UnityEngine;
public interface IMovementAbility 
{
    bool isActive { get; }
    bool isOnCooldown { get; }

    event Action Activated;
    event Action Completed;

    void Initialize(Rigidbody2D rb, PlayerMoveConfig moveConfig, Action<Vector2> applyVelocity);
    bool TryActivate(Vector2 direction);
    void FixedTick(float deltaTime);
    void Cancel();
}