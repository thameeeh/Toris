using UnityEngine;

[CreateAssetMenu(
    menuName = "WorldGen/Sites/Necromancer Grave Encounter Config",
    fileName = "NecromancerGraveEncounterConfig")]
public sealed class NecromancerGraveEncounterConfig : WorldSiteRuntimeConfig
{
    [SerializeField] private Necromancer necromancerPrefab;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField, Min(0f)] private float spawnDelaySeconds = 0.6f;
    [SerializeField] private bool beginEncounterWhenPlayerLeavesGrave = true;
    [SerializeField] private bool transformToFloaterOnEncounterStart = true;

    public Necromancer NecromancerPrefab => necromancerPrefab;
    public Vector2 SpawnOffset => spawnOffset;
    public float SpawnDelaySeconds => spawnDelaySeconds;
    public bool BeginEncounterWhenPlayerLeavesGrave => beginEncounterWhenPlayerLeavesGrave;
    public bool TransformToFloaterOnEncounterStart => transformToFloaterOnEncounterStart;
}
