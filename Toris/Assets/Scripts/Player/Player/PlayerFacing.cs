using UnityEngine;

// PURPOSE:
// - Gameplay-owned facing memory for the player
// - Used by dash / aiming / future gameplay logic
// - Prevents gameplay from asking animation what direction the player is facing

public class PlayerFacing : MonoBehaviour
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [SerializeField] private Vector2 _currentFacing = Vector2.down;

    public Vector2 CurrentFacing => _currentFacing;

    public void SetFacing(Vector2 direction)
    {
        if (direction.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            return;

        _currentFacing = direction.normalized;
    }
}