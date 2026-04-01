public readonly struct WorldEncounterPackage
{
    public readonly string PackageId;
    public readonly WorldEncounterServices Services;
    public readonly WorldEncounterOccupantPolicy OccupantPolicy;
    public readonly WorldEncounterPackageState State;
    public readonly WorldSiteRuntimeConfig RuntimeConfig;

    public WorldEncounterPackage(
        string packageId,
        WorldEncounterServices services,
        WorldEncounterOccupantPolicy occupantPolicy,
        WorldEncounterPackageState state,
        WorldSiteRuntimeConfig runtimeConfig)
    {
        PackageId = packageId;
        Services = services;
        OccupantPolicy = occupantPolicy;
        State = state;
        RuntimeConfig = runtimeConfig;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(PackageId) &&
        Services != null &&
        OccupantPolicy != null &&
        State.IsValid &&
        RuntimeConfig != null;

    public T GetConfig<T>() where T : WorldSiteRuntimeConfig
    {
        return RuntimeConfig as T;
    }
}
