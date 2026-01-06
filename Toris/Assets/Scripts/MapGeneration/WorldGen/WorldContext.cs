using UnityEngine;

public sealed class WorldContext
{
    public readonly WorldGenProfile Profile;

    public int RunSeed { get; private set; }
    public BiomeInstance ActiveBiome { get; private set; }

    public NoiseContext Noise { get; private set; }
    public BiomeMask Mask { get; private set; }
    public FeatureStamps Stamps { get; private set; }
    public GateRegistry Gates { get; private set; }

    public AnimationCurve DangerCurve => Profile.dangerCurve;

    public WorldContext(WorldGenProfile profile)
    {
        Profile = profile;
        RunSeed = profile.seed;

        Mask = new BiomeMask(profile);
        Stamps = new FeatureStamps();
        Gates = new GateRegistry();

        // Temporary; must call BindBiome before generating.
        ActiveBiome = new BiomeInstance(0, RunSeed, Vector2Int.zero, profile.worldRadiusTiles);
        Noise = new NoiseContext(ActiveBiome.Seed);
    }

    public void BindBiome(BiomeInstance biome)
    {
        ActiveBiome = biome;
        Noise = new NoiseContext(biome.Seed);

        Stamps.Clear();
        Gates.Clear();

        BiomeFeatureBuilder.Build(this);
    }
}

public struct WorldSignals
{
    public float dist01;
    public float danger01;

    public float forest01;
    public float variation01;
    public float lake01;

    public float road01;
}
