using UnityEngine;

public struct SfxPlayRequest
{
    // Optional overrides / multipliers.
    public float volumeMultiplier;   // default 1
    public float pitchOffset;        // default 0 (added after random pitch)
    public float pitchMultiplier;    // default 1

    // Optional runtime routing adjustments.
    public bool force2D;             // if true, spatialBlend becomes 0

    // Optional: if provided, overrides position even for non-attached calls.
    public Vector3? explicitWorldPosition;

    public static SfxPlayRequest Default => new SfxPlayRequest
    {
        volumeMultiplier = 1f,
        pitchOffset = 0f,
        pitchMultiplier = 1f,
        force2D = false,
        explicitWorldPosition = null
    };
}
