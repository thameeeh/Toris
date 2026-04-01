using UnityEngine;

public sealed class WorldSceneServices : IWorldNavigationService
{
    public Grid Grid { get; }
    private readonly TileNavWorld tileNavWorld;

    public WorldSceneServices(Grid grid, TileNavWorld tileNavWorld)
    {
        Grid = grid;
        this.tileNavWorld = tileNavWorld;
    }

    public Vector3 GetCellCenterWorld(Vector2Int tile)
    {
        if (Grid == null)
            return Vector3.zero;

        return Grid.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
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
