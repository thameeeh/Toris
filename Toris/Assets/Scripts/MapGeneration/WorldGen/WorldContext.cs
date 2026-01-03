using UnityEngine;

public sealed class WorldContext
{
    public readonly int Seed;
    public readonly float WorldRadiusTiles;
    public readonly Vector2 SpawnPosTiles;
    public readonly AnimationCurve DangerCurve;
    public readonly NoiseContext Noise;
    public readonly WorldGenProfile Profile;
    public readonly RoadNetwork Roads;

    public WorldContext(WorldGenProfile profile)
    {
        Profile = profile;
        Seed = profile.seed;
        WorldRadiusTiles = Mathf.Max(1f, profile.worldRadiusTiles);
        SpawnPosTiles = profile.spawnPosTiles;
        DangerCurve = profile.dangerCurve;
        Noise = new NoiseContext(profile.seed);
        Roads = new RoadNetwork(this);
    }
}

public struct WorldSignals
{
    public float dist01;
    public float danger01;

    public float islandMask01;
    public float forest01;

    public float variation01;
    public float lake01;

    public float road01;
}
