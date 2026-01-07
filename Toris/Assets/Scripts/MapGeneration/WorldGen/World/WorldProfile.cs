using UnityEngine;

[CreateAssetMenu(menuName = "WorldGen/World Profile", fileName = "WorldProfile")]
public sealed class WorldProfile : ScriptableObject
{
    [Header("World")]
    public int seed = 12345;
    public float worldRadiusTiles = 1500f;
    public Vector2 spawnPosTiles = Vector2.zero;

    [Header("Chunking")]
    public int chunkSize = 32;
    public int viewDistanceChunks = 2;

    [Header("Progression")]
    public AnimationCurve dangerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
}
