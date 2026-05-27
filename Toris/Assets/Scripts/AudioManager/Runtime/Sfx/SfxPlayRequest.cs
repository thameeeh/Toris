using UnityEngine;

public struct SfxPlayRequest
{
    // Optional overrides / multipliers.
    public float volumeMultiplier;   // default 1
    public float pitchOffset;        // default 0 (added after random pitch)
    public float pitchMultiplier;    // default 1
    public float fadeInSeconds;      // default 0

    // Optional runtime routing adjustments.
    public bool force2D;             // if true, spatialBlend becomes 0
    public bool allowDuringGameplayPause; // UI feedback may still play while world/gameplay audio is suspended

    // Optional: if provided, overrides position even for non-attached calls.
    public Vector3? explicitWorldPosition;

    public static SfxPlayRequest Default => new SfxPlayRequest
    {
        volumeMultiplier = 1f,
        pitchOffset = 0f,
        pitchMultiplier = 1f,
        fadeInSeconds = 0f,
        force2D = false,
        allowDuringGameplayPause = false,
        explicitWorldPosition = null
    };
}
