using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Encounters/Wolf Den Encounter Config",
    fileName = "WolfDenEncounterConfig")]
public sealed class WolfDenEncounterConfig : WorldSiteRuntimeConfig
{
    [Header("Prefabs")]
    [SerializeField] private Wolf leaderPrefab;
    [SerializeField] private Wolf minionPrefab;

    [Header("Occupant Policy")]
    [SerializeField] private WorldEncounterOccupantPolicy occupantPolicy = new();

    [SerializeField, HideInInspector] private float leaderRespawnDelay = 6f;
    [SerializeField, HideInInspector] private int spawnRadius = 4;
    [SerializeField, HideInInspector] private bool keepChasingWolvesOnUnload = true;
    [SerializeField, HideInInspector] private float keepChaseIfWithinPlayerRange = 40f;
    [SerializeField, HideInInspector] private float homeRadius = 8f;
    [SerializeField, HideInInspector] private bool occupantPolicyMigrated;

    [Header("Den Alert")]
    [SerializeField] private float denAlertDuration = 4f;
    [SerializeField] private float alertLevelDecayDelay = 2.5f;
    [SerializeField] private float alertLevelDecayRate = 0.35f;
    [SerializeField] private float alertLevelPerHit = 1f;
    [SerializeField] private float maxAlertLevel = 4f;

    [Header("Den Alert Escalation")]
    [SerializeField] private float investigateStandBonusPerAlert = 0.35f;
    [SerializeField] private float investigateBaseStepsFromDen = 1f;
    [SerializeField] private float investigateExtraStepsPerAlert = 1f;
    [SerializeField] private int investigatePointSearchRadius = 6;

    [Header("Max Alert Response")]
    [SerializeField] private bool howlAtMaxAlert = true;
    [SerializeField] private float alertLevelAfterHowl = 0f;

    public Wolf LeaderPrefab => leaderPrefab;
    public Wolf MinionPrefab => minionPrefab;
    public WorldEncounterOccupantPolicy OccupantPolicy
    {
        get
        {
            EnsureOccupantPolicy();
            return occupantPolicy;
        }
    }

    public float DenAlertDuration => denAlertDuration;
    public float AlertLevelDecayDelay => alertLevelDecayDelay;
    public float AlertLevelDecayRate => alertLevelDecayRate;
    public float AlertLevelPerHit => alertLevelPerHit;
    public float MaxAlertLevel => maxAlertLevel;

    public float InvestigateStandBonusPerAlert => investigateStandBonusPerAlert;
    public float InvestigateBaseStepsFromDen => investigateBaseStepsFromDen;
    public float InvestigateExtraStepsPerAlert => investigateExtraStepsPerAlert;
    public int InvestigatePointSearchRadius => investigatePointSearchRadius;

    public bool HowlAtMaxAlert => howlAtMaxAlert;
    public float AlertLevelAfterHowl => alertLevelAfterHowl;

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureOccupantPolicy();
        occupantPolicy.Validate();
    }
#endif

    private void EnsureOccupantPolicy()
    {
        occupantPolicy ??= new WorldEncounterOccupantPolicy();
        if (occupantPolicyMigrated)
            return;

        occupantPolicy.ApplyLegacyValues(
            leaderRespawnDelay,
            spawnRadius,
            keepChasingWolvesOnUnload,
            keepChaseIfWithinPlayerRange,
            homeRadius);

        occupantPolicyMigrated = true;
    }
}
