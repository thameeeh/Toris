using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Gate Site Rule",
    fileName = "GateSitePlacementRuleDefinition")]
public sealed class GateSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    public override void BuildSites(WorldContext ctx)
    {
        if (ctx.Biome == null)
            return;

        int gateSize = Mathf.Max(1, ctx.Biome.gateSize);

        var gateAnchorTiles = ctx.RoadAnchors.GateAnchorTiles;
        for (int i = 0; i < gateAnchorTiles.Count; i++)
        {
            Vector2Int gateCenterTile = gateAnchorTiles[i];

            if (ctx.Biome.gateGroundTile != null)
                ctx.Stamps.StampRectGround(gateCenterTile, gateSize, gateSize, ctx.Biome.gateGroundTile);

            ctx.Gates.AddGateFootprint(gateCenterTile, gateSize);
            ctx.AddGateSite(gateCenterTile);
        }
    }
}