using UnityEngine;
using UnityEngine.Profiling;

public sealed class ChunkGenerator
{
    private readonly WorldContext ctx;
    private readonly TileResolver resolver;

    public ChunkGenerator(WorldContext ctx)
    {
        this.ctx = ctx;
        resolver = new TileResolver();
    }

    public ChunkResult GenerateChunk(Vector2Int chunkCoord)
    {
        int size = ctx.Profile.chunkSize;
        ChunkResult result = new ChunkResult(chunkCoord, size);

        if (ctx.Profile.enableRoads)
            ctx.Roads.BakeChunk(chunkCoord, size);

        int baseX = chunkCoord.x * size;
        int baseY = chunkCoord.y * size;

        for (int ly = 0; ly < size; ly++)
        {
            for (int lx = 0; lx < size; lx++)
            {
                Vector2Int tilePos = new Vector2Int(baseX + lx, baseY + ly);
                TileResult t = resolver.Resolve(tilePos, ctx);

                int idx = ChunkResult.Index(lx, ly, size);
                result.ground[idx] = t.ground;
                result.water[idx] = t.water;
                result.decor[idx] = t.decor;
            }
        }

        return result;
    }
}
