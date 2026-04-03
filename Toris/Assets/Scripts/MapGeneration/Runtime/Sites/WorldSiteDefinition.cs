using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Sites/World Site Definition",
    fileName = "WorldSiteDefinition")]
public sealed class WorldSiteDefinition : ScriptableObject
{
    [SerializeField] private string siteId;
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool skipIfConsumed;
    [SerializeField] private uint spawnSalt;
    [SerializeField] private WorldSiteRuntimeConfig runtimeConfig;

    public WorldSiteRuntimeConfig RuntimeConfig => runtimeConfig;
    public string SiteId => siteId;
    public GameObject Prefab => prefab;
    public bool SkipIfConsumed => skipIfConsumed;
    public uint SpawnSalt => spawnSalt;

    public bool IsValid => prefab != null && !string.IsNullOrWhiteSpace(siteId);
}