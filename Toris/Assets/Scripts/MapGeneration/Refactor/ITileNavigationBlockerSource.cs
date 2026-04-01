using UnityEngine;

public interface ITileNavigationBlockerSource : ITileNavigationContributionSource
{
    bool IsNavigationBlocked(Vector2Int tile);
}
