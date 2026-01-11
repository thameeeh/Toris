using UnityEngine;

public sealed class WorldContext
{
    public readonly WorldProfile World;

    //public int RunSeed { get; private set; } KEEP FOR RUN MODIFIERS LATER

    public BiomeInstance ActiveBiome { get; private set; }
    public BiomeDefinition ActiveDef { get; private set; }
    public BiomeProfile Biome => ActiveDef != null ? ActiveDef.profile : null;

    public NoiseContext Noise { get; private set; }
    public BiomeMask Mask { get; private set; }
    public FeatureStamps Stamps { get; private set; }
    public GateRegistry Gates { get; private set; }
    public ChunkStateStore ChunkStates { get; private set; }

    public AnimationCurve DangerCurve => World.dangerCurve;
    public WorldContext(WorldProfile world)
    {
        World = world;
        //RunSeed = world.seed;

        Mask = new BiomeMask();
        Stamps = new FeatureStamps();
        Gates = new GateRegistry();
        ChunkStates = new ChunkStateStore();

        ActiveBiome = new BiomeInstance(0, World.seed, Vector2Int.zero, world.worldRadiusTiles);
        Noise = new NoiseContext(ActiveBiome.Seed);
    }

    public void BindBiome(BiomeDefinition def, BiomeInstance biome)
    {
        ActiveDef = def;
        ActiveBiome = biome;
        Noise = new NoiseContext(biome.Seed);

        Stamps.Clear();
        Gates.Clear();
        ChunkStates.Clear();

        ActiveDef?.BuildFeatures(this);
    }
}

public struct WorldSignals
{
    public float dist01;
    public float danger01;

    public float vegetation01;
    public float variation01;
    public float lake01;

    public float road01;
}
