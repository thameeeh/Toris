using UnityEngine;

/// <summary>
/// Reports a quest fact when the player enters a trigger, such as visiting or clearing a world site.
/// Use this for area objectives where entering a trigger should progress Pixel Crushers quests.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class QuestFactTriggerReporter : MonoBehaviour
{
    [Tooltip("Fact type to report. Common choices are VisitSite or ClearSite.")]
    [SerializeField] private QuestFactType _factType = QuestFactType.VisitSite;
    [Tooltip("Stable exact target ID. Example: GraveyardGate01 or WolfDen03.")]
    [SerializeField] private string _exactId = string.Empty;
    [Tooltip("Optional broader target type/tag. Example: Graveyard, WolfDen, RoadGate.")]
    [SerializeField] private string _typeOrTag = string.Empty;
    [Tooltip("Optional context for matching quest rules. Example: Plains, MainArea, Tutorial.")]
    [SerializeField] private string _contextId = string.Empty;
    [Tooltip("Amount of progress this trigger gives to matching quest rules.")]
    [SerializeField, Min(1)] private int _amount = 1;
    [Tooltip("If enabled, this trigger reports only once per scene lifetime.")]
    [SerializeField] private bool _reportOnce = true;
    [Tooltip("If enabled, only colliders with Player Tag can trigger this fact.")]
    [SerializeField] private bool _requirePlayerTag = true;
    [Tooltip("Tag required when Require Player Tag is enabled.")]
    [SerializeField] private string _playerTag = "Player";

    private bool _reported;

    private void Reset()
    {
        if (TryGetComponent(out Collider2D triggerCollider))
            triggerCollider.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_reportOnce && _reported)
            return;

        if (_requirePlayerTag && (string.IsNullOrWhiteSpace(_playerTag) || !other.CompareTag(_playerTag)))
            return;

        Report();
    }

    public void Report()
    {
        if (_reportOnce && _reported)
            return;

        PixelCrushersQuestFactReporter.Report(new QuestFact(_factType, _exactId, _typeOrTag, _amount, _contextId));
        _reported = true;
    }
}
