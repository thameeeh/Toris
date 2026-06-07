using UnityEngine;

[CreateAssetMenu(menuName = "Outland Haven/WorldGen/World Profile", fileName = "WorldProfile")]
public sealed class WorldProfile : ScriptableObject
{
    private const int MinGeneratedSeed = 1;

    [Header("World")]
    public bool autoGenerateSeed = false;
    public int seed = 12345;
    public float worldRadiusTiles = 1500f;
    public Vector2 spawnPosTiles = Vector2.zero;

    [Header("Chunking")]
    public int chunkSize = 32;
    public int viewDistanceChunks = 2;

    [Header("Progression")]
    public AnimationCurve dangerCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public int ResolveRunSeed()
    {
        if (!autoGenerateSeed)
            return seed;

        return Random.Range(MinGeneratedSeed, int.MaxValue);
    }
}
