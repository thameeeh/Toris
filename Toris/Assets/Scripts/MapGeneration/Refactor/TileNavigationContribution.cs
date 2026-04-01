public readonly struct TileNavigationContribution
{
    public static readonly TileNavigationContribution None = new(false);
    public static readonly TileNavigationContribution Blocked = new(true);

    public bool BlocksNavigation { get; }

    public TileNavigationContribution(bool blocksNavigation)
    {
        BlocksNavigation = blocksNavigation;
    }
}
