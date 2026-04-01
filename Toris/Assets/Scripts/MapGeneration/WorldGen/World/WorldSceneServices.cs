using UnityEngine;

public sealed class WorldSceneServices : IWorldNavigationService
{
    public Grid Grid { get; }
    public TileNavWorld TileNavWorld { get; }

    public WorldSceneServices(Grid grid, TileNavWorld tileNavWorld)
    {
        Grid = grid;
        TileNavWorld = tileNavWorld;
    }

    public Vector3 GetCellCenterWorld(Vector2Int tile)
    {
        if (Grid == null)
            return Vector3.zero;

        return Grid.GetCellCenterWorld(new Vector3Int(tile.x, tile.y, 0));
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        if (TileNavWorld == null)
            return default;

        return TileNavWorld.WorldToCell(worldPosition);
    }

    public Vector3 CellToWorldCenter(Vector2Int cell)
    {
        if (TileNavWorld == null)
            return Vector3.zero;

        return TileNavWorld.CellToWorldCenter(cell);
    }

    public bool IsWalkableCell(Vector2Int cell)
    {
        return TileNavWorld != null && TileNavWorld.IsWalkableCell(cell);
    }
}
