using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

public sealed class SceneTransitionService : MonoBehaviour, ISceneTransitionService, IRunGateTransitionService
{
    public static SceneTransitionService Instance { get; private set; }

    [Header("Optional hooks (UI fade, SFX, etc.)")]
    public UnityEvent onTransitionStart;
    public UnityEvent onTransitionEnd;

    private bool _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool IsLoading => _isLoading;

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (_isLoading) return;
        StartCoroutine(LoadRoutine(sceneName, mode));
    }

    public void UseRunGate(string sceneA, string sceneB)
    {
        string current = SceneManager.GetActiveScene().name;

        if (current == sceneA)
        {
            LoadScene(sceneB);
            return;
        }

        if (current == sceneB)
        {
            LoadScene(sceneA);
            return;
        }

        Debug.LogWarning(
            $"[SceneTransitionService] Current scene '{current}' does not match '{sceneA}' or '{sceneB}'.",
            this);
    }

    private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode)
    {
        _isLoading = true;
        onTransitionStart?.Invoke();

        yield return null;

        var op = SceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
            _isLoading = false;
            yield break;
        }

        while (!op.isDone)
            yield return null;

        onTransitionEnd?.Invoke();
        _isLoading = false;
    }
}
