using UnityEngine;

public sealed class GateTransitionServiceAdapter : IGateTransitionService
{
    private readonly WorldGenRunner worldGenRunner;

    public GateTransitionServiceAdapter(WorldGenRunner worldGenRunner)
    {
        this.worldGenRunner = worldGenRunner;
    }

    public void UseGate(Vector2Int gateTile)
    {
        if (worldGenRunner == null)
            return;

        worldGenRunner.UseGate(gateTile);
    }
}