public readonly struct WorldEncounterPackage
{
    public readonly string PackageId;
    public readonly WorldEncounterServices Services;
    public readonly WorldEncounterOccupantPolicy OccupantPolicy;
    public readonly WorldSiteRuntimeConfig RuntimeConfig;

    public WorldEncounterPackage(
        string packageId,
        WorldEncounterServices services,
        WorldEncounterOccupantPolicy occupantPolicy,
        WorldSiteRuntimeConfig runtimeConfig)
    {
        PackageId = packageId;
        Services = services;
        OccupantPolicy = occupantPolicy;
        RuntimeConfig = runtimeConfig;
    }

    public bool IsValid =>
        !string.IsNullOrWhiteSpace(PackageId) &&
        Services != null &&
        OccupantPolicy != null &&
        RuntimeConfig != null;

    public T GetConfig<T>() where T : WorldSiteRuntimeConfig
    {
        return RuntimeConfig as T;
    }
}
