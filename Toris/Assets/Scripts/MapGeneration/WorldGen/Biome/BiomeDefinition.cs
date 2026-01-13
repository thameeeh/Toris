using UnityEngine;

public abstract class BiomeDefinition : ScriptableObject
{
    public BiomeProfile profile;

    public abstract void BuildFeatures(WorldContext ctx);

    public virtual bool TryResolveTile(Vector2Int worldTile, WorldContext ctx, out TileResult result)
    {
        result = default;
        return false;
    }
}
