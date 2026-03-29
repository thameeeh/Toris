using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    menuName = "WorldGen/Biomes/Site Rules/Gate Site Rule",
    fileName = "GateSitePlacementRuleDefinition")]
public sealed class GateSitePlacementRuleDefinition : SitePlacementRuleDefinition
{
    [SerializeField] private WorldSiteDefinition gateSiteDefinition;
    [SerializeField] private TileBase gateGroundTile;
    [SerializeField]
    [Min(1)] private int gateSize = 7;
    public override void BuildSites(WorldContext ctx)
    {
        if (gateSiteDefinition == null || !gateSiteDefinition.IsValid)
            return;

        int resolvedGateSize = Mathf.Max(1, gateSize);

        var gateAnchorTiles = ctx.RoadAnchors.GateAnchorTiles;
        for (int i = 0; i < gateAnchorTiles.Count; i++)
        {
            Vector2Int gateCenterTile = gateAnchorTiles[i];

            if (gateGroundTile != null)
                ctx.Stamps.StampRectGround(gateCenterTile, resolvedGateSize, resolvedGateSize, gateGroundTile);

            ctx.RegisterSite(gateSiteDefinition, gateCenterTile);
        }
    }
}