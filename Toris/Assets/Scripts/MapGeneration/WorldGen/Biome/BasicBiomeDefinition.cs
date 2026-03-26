using UnityEngine;

[CreateAssetMenu(menuName = "WorldGen/Biomes/Basic Biome", fileName = "BasicBiomeDefinition")]
public sealed class BasicBiomeDefinition : BiomeDefinition
{
    [Header("Build Steps")]
    [SerializeField] private BiomeBuildStepDefinition[] buildSteps;

    public override void BuildFeatures(WorldContext ctx)
    {
        if (buildSteps == null || buildSteps.Length == 0)
        {
            Debug.LogWarning($"{name} has no biome build steps assigned.", this);
            return;
        }

        for (int i = 0; i < buildSteps.Length; i++)
        {
            BiomeBuildStepDefinition buildStep = buildSteps[i];
            if (buildStep == null)
                continue;

            buildStep.Build(ctx);
        }
    }
}