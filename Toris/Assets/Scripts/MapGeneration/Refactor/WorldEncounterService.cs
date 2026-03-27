public sealed class WorldEncounterServices
{
    public IWorldNavigationService NavigationService { get; }
    public IPlayerLocator PlayerLocator { get; }
    public IEnemySpawnService EnemySpawnService { get; }

    public WorldEncounterServices(
        IWorldNavigationService navigationService,
        IPlayerLocator playerLocator,
        IEnemySpawnService enemySpawnService)
    {
        NavigationService = navigationService;
        PlayerLocator = playerLocator;
        EnemySpawnService = enemySpawnService;
    }
}