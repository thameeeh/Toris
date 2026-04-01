using UnityEngine;

public interface IWorldNavigationService
{
    Vector2Int WorldToCell(Vector3 worldPosition);
    Vector3 CellToWorldCenter(Vector2Int cell);
    bool IsWalkableCell(Vector2Int cell);
}