using UnityEngine;

public interface ITileNavigationBlockerSource
{
    bool IsNavigationBlocked(Vector2Int tile);
}
