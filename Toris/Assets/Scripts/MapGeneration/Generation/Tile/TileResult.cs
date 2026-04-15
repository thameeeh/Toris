using UnityEngine;
using UnityEngine.Tilemaps;

public struct TileResult
{
    public TileBase ground;
    public TileBase water;
    public TileBase decoration;
    public TileBase obstacle;
    public TileBase canopy;

    public bool HasWater => water != null;
    public bool HasDecoration => decoration != null;
    public bool HasObstacle => obstacle != null;
    public bool HasCanopy => canopy != null;
}
