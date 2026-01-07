using UnityEngine;
using UnityEngine.Tilemaps;

public struct TileResult
{
    public TileBase ground;
    public TileBase water;
    public TileBase decor;

    public bool HasWater => water != null;
    public bool HasDecor => decor != null;
}
