using UnityEngine;

/// <summary>
/// Inspector-friendly quest fact source for UnityEvents, buttons, and one-off world objects.
/// Use this when a gameplay script should not know anything about Pixel Crushers or quest rules.
/// </summary>
[AddComponentMenu("Toris/Quest/Quest Fact Manual Reporter")]
public class QuestFactManualReporter : MonoBehaviour
{
    [Tooltip("Fact type to report. Examples: Deliver, InteractWorldObject, BiomeReached, Explore.")]
    [SerializeField] private QuestFactType _factType = QuestFactType.InteractWorldObject;
    [Tooltip("Stable exact target ID. Example: AncientGate01, SmithDelivery01, GraveyardBiome.")]
    [SerializeField] private string _exactId = string.Empty;
    [Tooltip("Optional broader target type/tag. Example: Gate, Delivery, Graveyard, Tutorial.")]
    [SerializeField] private string _typeOrTag = string.Empty;
    [Tooltip("Optional context for matching quest rules. Example: MainArea, Act1, SafeHaven.")]
    [SerializeField] private string _contextId = string.Empty;
    [Tooltip("Amount of progress this report gives to matching quest rules.")]
    [SerializeField, Min(1)] private int _amount = 1;
    [Tooltip("If enabled, this component reports only once per scene lifetime.")]
    [SerializeField] private bool _reportOnce = true;

    private bool _reported;

    public void Report()
    {
        if (_reportOnce && _reported)
            return;

        PixelCrushersQuestFactReporter.Report(new QuestFact(_factType, _exactId, _typeOrTag, _amount, _contextId));
        _reported = true;
    }
}
