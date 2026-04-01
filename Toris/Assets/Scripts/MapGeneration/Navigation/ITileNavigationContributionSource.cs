using UnityEngine;

public interface ITileNavigationContributionSource
{
    TileNavigationContribution GetNavigationContribution(Vector2Int tile);
}
