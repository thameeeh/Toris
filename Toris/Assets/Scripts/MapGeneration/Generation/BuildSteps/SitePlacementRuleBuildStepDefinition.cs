using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Build Steps/Site Placement Rule Step",
    fileName = "SitePlacementRuleBuildStepDefinition")]
public sealed class SitePlacementRuleBuildStepDefinition : BiomeBuildStepDefinition
{
    [SerializeField] private SitePlacementRuleDefinition[] sitePlacementRules;

    public override void Build(WorldContext ctx)
    {
        if (sitePlacementRules == null || sitePlacementRules.Length == 0)
            return;

        for (int i = 0; i < sitePlacementRules.Length; i++)
        {
            SitePlacementRuleDefinition sitePlacementRule = sitePlacementRules[i];
            if (sitePlacementRule == null)
                continue;

            sitePlacementRule.BuildSites(ctx);
        }
    }
}