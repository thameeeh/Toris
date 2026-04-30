using UnityEngine;

/// <summary>
/// Optional editor-facing switch for noisy quest fact logs.
/// Add this to the quest bootstrap object when fact-level diagnostics are needed.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersQuestFactReporterDebugSettings : MonoBehaviour
{
    [Tooltip("Logs every reported quest fact in the editor. Keep disabled unless diagnosing objective progress.")]
    [SerializeField] private bool _debugFacts;

    private void OnEnable()
    {
        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Apply();
    }
#endif

    private void Apply()
    {
        PixelCrushersQuestFactReporter.SetDebugFacts(_debugFacts);
    }
}
