using OutlandHaven.SaveSystem;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.SceneManagement;

// Captures the pre-run save that death respawn restores before applying penalties.
public sealed class RunStartCheckpointService : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private SaveManager _saveManager;
    [SerializeField] private GameSessionSO _gameSession;

    [Header("Scene Policy")]
    [SerializeField] private string _hubSceneName = "MainArea";
    [SerializeField] private string _runSceneName = "ProceduralTiles";
    [SerializeField] private bool _saveCheckpointBeforeRun = true;

    public void CaptureCheckpointIfRunStart(string sceneA, string sceneB)
    {
        if (!_saveCheckpointBeforeRun)
            return;

        string currentSceneName = SceneManager.GetActiveScene().name;
        string destinationSceneName = ResolveDestinationScene(currentSceneName, sceneA, sceneB);

        if (!SceneNameEquals(currentSceneName, _hubSceneName) || !SceneNameEquals(destinationSceneName, _runSceneName))
            return;

        ResolveDependencies();

        if (_saveManager == null)
        {
#if UNITY_EDITOR
            Debug.LogWarning("[RunStartCheckpointService] Missing SaveManager; run-start checkpoint was not saved.", this);
#endif
            return;
        }

        if (_gameSession != null && _saveManager.ActiveSession == null)
        {
            _saveManager.ActiveSession = _gameSession;
        }

        if (_saveManager.ActiveSession != null)
        {
            _saveManager.SaveGame(_saveManager.ActiveSession.ActiveSaveSlot);
        }
    }

    private void ResolveDependencies()
    {
        if (_saveManager == null || !IsSceneInstance(_saveManager))
        {
            _saveManager = FindFirstObjectByType<SaveManager>();
        }

        if (_gameSession == null)
        {
            _gameSession = GameSessionSO.LoadDefault();
        }
    }

    private static string ResolveDestinationScene(string currentSceneName, string sceneA, string sceneB)
    {
        if (SceneNameEquals(currentSceneName, sceneA))
            return sceneB;

        if (SceneNameEquals(currentSceneName, sceneB))
            return sceneA;

        return string.Empty;
    }

    private static bool SceneNameEquals(string lhs, string rhs)
    {
        return !string.IsNullOrWhiteSpace(lhs)
            && !string.IsNullOrWhiteSpace(rhs)
            && string.Equals(lhs.Trim(), rhs.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSceneInstance(Component component)
    {
        return component != null && component.gameObject.scene.IsValid();
    }
}
