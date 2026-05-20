using UnityEngine;

[CreateAssetMenu(fileName = "DeathPenaltyConfig", menuName = "Game/Player/Death Penalty Config")]
public sealed class DeathPenaltyConfigSO : ScriptableObject
{
    [Header("Progression")]
    [SerializeField, Range(0f, 1f)] private float _experienceLossPercent = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _goldLossPercent = 0.1f;

    [Header("Inventory")]
    [SerializeField, Range(0f, 1f)] private float _backpackItemLossPercent = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _potionItemLossPercent = 0.1f;
    [SerializeField] private bool _removeAtLeastOneItemWhenPossible = true;

    public float ExperienceLossPercent => Mathf.Clamp01(_experienceLossPercent);
    public float GoldLossPercent => Mathf.Clamp01(_goldLossPercent);
    public float BackpackItemLossPercent => Mathf.Clamp01(_backpackItemLossPercent);
    public float PotionItemLossPercent => Mathf.Clamp01(_potionItemLossPercent);
    public bool RemoveAtLeastOneItemWhenPossible => _removeAtLeastOneItemWhenPossible;
}
