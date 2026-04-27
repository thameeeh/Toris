using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reports a scene-level quest fact, usually when a scene or major gameplay area becomes active.
/// Use this for objectives like entering the overworld, visiting a hub, or reaching a named scene.
/// </summary>
public class QuestFactSceneReporter : MonoBehaviour
{
    [Tooltip("Fact type to report. Usually EnterScene.")]
    [SerializeField] private QuestFactType _factType = QuestFactType.EnterScene;
    [Tooltip("Stable exact scene or area ID. Leave blank to use the active Unity scene name if enabled below.")]
    [SerializeField] private string _exactId = string.Empty;
    [Tooltip("Optional broader scene/area type. Example: Overworld, SafeHub, Dungeon.")]
    [SerializeField] private string _typeOrTag = string.Empty;
    [Tooltip("Optional extra context for quest rules. Example: MainQuest, Tutorial, Act1.")]
    [SerializeField] private string _contextId = string.Empty;
    [Tooltip("Amount of progress this report gives to matching quest rules.")]
    [SerializeField, Min(1)] private int _amount = 1;
    [Tooltip("When Exact Id is blank, use SceneManager.GetActiveScene().name as the exact ID.")]
    [SerializeField] private bool _useActiveSceneNameWhenExactIdEmpty = true;
    [Tooltip("Automatically report when this component starts.")]
    [SerializeField] private bool _reportOnStart = true;
    [Tooltip("If enabled, this component reports only once per scene lifetime.")]
    [SerializeField] private bool _reportOnce = true;

    private bool _reported;

    private void Start()
    {
        if (_reportOnStart)
            Report();
    }

    public void Report()
    {
        if (_reportOnce && _reported)
            return;

        string resolvedExactId = ResolveExactId();
        PixelCrushersQuestFactReporter.Report(new QuestFact(_factType, resolvedExactId, _typeOrTag, _amount, _contextId));
        _reported = true;
    }

    private string ResolveExactId()
    {
        if (!string.IsNullOrWhiteSpace(_exactId))
            return _exactId;

        return _useActiveSceneNameWhenExactIdEmpty
            ? SceneManager.GetActiveScene().name
            : string.Empty;
    }
}
