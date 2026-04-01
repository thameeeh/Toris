using UnityEngine;

public sealed class WorldSceneServices : IWorldNavigationService
{
    private readonly Grid grid;
    private readonly TileNavWorld tileNavWorld;

    public WorldSceneServices(Grid grid, TileNavWorld tileNavWorld)
    {
        this.grid = grid;
        this.tileNavWorld = tileNavWorld;
    }

    public Vector3 GetCellCenterWorld(Vector2Int tile)
    {
        if (grid == null)
            return Vector3.zero;

        return grid.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        if (tileNavWorld == null)
            return default;

        return tileNavWorld.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        if (tileNavWorld == null)
            return Vector3.zero;

        return tileNavWorld.CellToWorldCenter(cell);
    }

    public bool IsWalkableCell(Vector2Int cell)
    {
        return tileNavWorld != null && tileNavWorld.IsWalkableCell(cell);
    }
}
