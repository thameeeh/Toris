using UnityEngine;

public static class PlayerMovementDirectionUtility
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const float ISOMETRIC_X_SCALE = 2f;

    public static Vector2 ToWorldAlignedDirection(Vector2 inputDirection)
    {
        if (inputDirection.sqrMagnitude < MIN_DIRECTION_SQR_MAGNITUDE)
            return Vector2.zero;

        inputDirection.x *= ISOMETRIC_X_SCALE;
        return inputDirection.normalized;
    }
}
