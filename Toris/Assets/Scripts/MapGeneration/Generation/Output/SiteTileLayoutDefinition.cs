using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    menuName = "WorldGen/Sites/Site Tile Layout Definition",
    fileName = "SiteTileLayoutDefinition")]
public sealed class SiteTileLayoutDefinition : ScriptableObject
{
    [SerializeField] private List<SiteTileLayoutCell> cells = new();

    public IReadOnlyList<SiteTileLayoutCell> Cells => cells;
}

[System.Serializable]
public struct SiteTileLayoutCell
{
    public Vector2Int offset;
    public TileBase ground;
    public TileBase water;
    public TileBase decoration;
    public TileBase obstacle;
    public TileBase canopy;
}
