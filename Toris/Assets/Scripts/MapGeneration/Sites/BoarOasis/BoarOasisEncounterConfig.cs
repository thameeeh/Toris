using UnityEngine;

[CreateAssetMenu(
    menuName = "Outland Haven/WorldGen/Encounters/Boar Oasis Encounter Config",
    fileName = "BoarOasisEncounterConfig")]
public sealed class BoarOasisEncounterConfig : WorldSiteRuntimeConfig, IWorldEncounterPackageConfig
{
    private const string DefaultPackageId = "boar_oasis";

    [Header("Prefab")]
    [SerializeField] private Boar boarPrefab;

    [Header("Count")]
    [SerializeField, Min(0)] private int minBoarCount = 2;
    [SerializeField, Min(0)] private int maxBoarCount = 2;

    [Header("Occupant Policy")]
    [SerializeField] private WorldEncounterOccupantPolicy occupantPolicy = new();

    public Boar BoarPrefab => boarPrefab;
    public int MinBoarCount => minBoarCount;
    public int MaxBoarCount => maxBoarCount;
    public string PackageId => DefaultPackageId;
    public WorldEncounterOccupantPolicy OccupantPolicy
    {
        get
        {
            EnsureOccupantPolicy();
            return occupantPolicy;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minBoarCount = Mathf.Max(0, minBoarCount);
        maxBoarCount = Mathf.Max(minBoarCount, maxBoarCount);
        EnsureOccupantPolicy();
        occupantPolicy.Validate();
    }
#endif

    private void EnsureOccupantPolicy()
    {
        occupantPolicy ??= new WorldEncounterOccupantPolicy();
    }
}
