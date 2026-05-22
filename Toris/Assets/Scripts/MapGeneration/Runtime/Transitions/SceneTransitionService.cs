using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class SceneTransitionService : MonoBehaviour, IRunGateTransitionService
{
    public static SceneTransitionService Instance { get; private set; }

    private const string DefaultLoadingMessage = "Loading";
    [Header("Optional hooks (UI fade, SFX, etc.)")]
    public UnityEvent onTransitionStart;
    public UnityEvent onTransitionEnd;

    [Header("Loading Screen")]
    [SerializeField] private bool showLoadingScreen = true;
    [SerializeField] private VisualTreeAsset loadingOverlayTemplate;
    [SerializeField] private PanelSettings loadingOverlayPanelSettings;
    [SerializeField] private Sprite[] loadingBackgrounds;
    [SerializeField] private bool randomizeLoadingBackgrounds = true;
    [SerializeField] private Color fallbackBackgroundColor = Color.black;
    [SerializeField, Range(1f, 1.08f)] private float loadingBackgroundOverscan = 1.02f;
    [SerializeField, Range(0f, 1f)] private float backgroundDimAlpha = 0.35f;
    [SerializeField] private string loadingMessage = DefaultLoadingMessage;
    [SerializeField, Min(0.05f)] private float dotIntervalSeconds = 0.35f;
    [SerializeField, Min(1)] private int activationDotCount = 3;
    [SerializeField, Min(0f)] private float fadeInSeconds = 0.35f;
    [SerializeField, Min(0f)] private float blackHoldSeconds = 0.25f;
    [SerializeField, Min(0f)] private float loadingContentFadeInSeconds = 0.35f;
    [SerializeField, Min(0f)] private float fadeOutSeconds = 0.35f;
    [SerializeField, Min(0f)] private float minimumDisplaySeconds = 10f;
    [SerializeField, Min(0f)] private float postLoadHoldSeconds = 0.15f;

    private readonly SceneUiInputSuspender _sceneUiInputSuspender = new SceneUiInputSuspender();
    private bool _isLoading;
    private SceneLoadingOverlay _loadingOverlay;
    private int _nextBackgroundIndex;
    private int _lastBackgroundIndex = -1;

    public bool IsLoading => _isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (showLoadingScreen)
        {
            EnsureLoadingOverlay();
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        _sceneUiInputSuspender.Resume();
        _loadingOverlay?.Dispose();
        _loadingOverlay = null;
        Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (_isLoading)
            return;

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

        SceneLoadingOverlay overlay = showLoadingScreen ? EnsureLoadingOverlay() : null;
        int resolvedActivationDotCount = Mathf.Max(1, activationDotCount);
        float resolvedDotInterval = Mathf.Max(0.05f, dotIntervalSeconds);
        float activationReadyTime = Time.realtimeSinceStartup;
        float minimumDisplayEndTime = Time.realtimeSinceStartup;

        AsyncOperation op;

        if (overlay != null)
        {
            _sceneUiInputSuspender.Suspend();
            overlay.Show(CreateLoadingOverlaySettings(resolvedDotInterval, resolvedActivationDotCount));

            yield return FadeCover(overlay, 1f, fadeInSeconds);
            overlay.ResetLoadingContent();

            op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
                overlay.Hide();
                _sceneUiInputSuspender.Resume();
                onTransitionEnd?.Invoke();
                _isLoading = false;
                yield break;
            }

            op.allowSceneActivation = false;

            yield return HoldOverlay(overlay, blackHoldSeconds);
            yield return FadeLoadingContent(overlay, 1f, loadingContentFadeInSeconds);
            overlay.SetCoverAlpha(0f);
            overlay.StartLoadingAnimation();
            yield return null;

            float loadingVisibleTime = Time.realtimeSinceStartup;
            activationReadyTime = loadingVisibleTime + resolvedDotInterval * Mathf.Max(0, resolvedActivationDotCount - 1);
            minimumDisplayEndTime = loadingVisibleTime + Mathf.Max(0f, minimumDisplaySeconds);
        }
        else
        {
            op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
                _sceneUiInputSuspender.Resume();
                onTransitionEnd?.Invoke();
                _isLoading = false;
                yield break;
            }
        }

        if (overlay != null)
        {
            op.allowSceneActivation = false;

            while (op.progress < 0.9f
                   || Time.realtimeSinceStartup < activationReadyTime
                   || Time.realtimeSinceStartup < minimumDisplayEndTime)
            {
                _sceneUiInputSuspender.Suspend();
                overlay.Tick();
                yield return null;
            }

            overlay.CompleteLoading();
            op.allowSceneActivation = true;
        }

        while (!op.isDone)
        {
            if (overlay != null)
            {
                _sceneUiInputSuspender.Suspend();
            }

            overlay?.Tick();
            yield return null;
        }

        yield return null;

        if (overlay != null && postLoadHoldSeconds > 0f)
        {
            float hideAt = Time.realtimeSinceStartup + postLoadHoldSeconds;
            while (Time.realtimeSinceStartup < hideAt)
            {
                _sceneUiInputSuspender.Suspend();
                overlay.Tick();
                yield return null;
            }
        }

        if (overlay != null)
        {
            yield return FadeOverlay(overlay, 0f, fadeOutSeconds);
            overlay.Hide();
            _sceneUiInputSuspender.Resume();
        }

        onTransitionEnd?.Invoke();
        _isLoading = false;
    }

    private SceneLoadingOverlay EnsureLoadingOverlay()
    {
        if (_loadingOverlay != null)
            return _loadingOverlay;

        _loadingOverlay = new SceneLoadingOverlay(loadingOverlayTemplate, loadingOverlayPanelSettings);
        _sceneUiInputSuspender.SetExcludedDocument(_loadingOverlay.Document);
        return _loadingOverlay;
    }

    private SceneLoadingOverlaySettings CreateLoadingOverlaySettings(
        float resolvedDotInterval,
        int resolvedActivationDotCount)
    {
        return new SceneLoadingOverlaySettings
        {
            Background = GetNextLoadingBackground(),
            FallbackColor = fallbackBackgroundColor,
            BackgroundOverscan = loadingBackgroundOverscan,
            DimAlpha = backgroundDimAlpha,
            LoadingMessage = loadingMessage,
            DotIntervalSeconds = resolvedDotInterval,
            ActivationDotCount = resolvedActivationDotCount
        };
    }

    private Sprite GetNextLoadingBackground()
    {
        if (loadingBackgrounds == null || loadingBackgrounds.Length == 0)
            return null;

        if (randomizeLoadingBackgrounds)
            return GetRandomLoadingBackground();

        for (int i = 0; i < loadingBackgrounds.Length; i++)
        {
            int index = _nextBackgroundIndex % loadingBackgrounds.Length;
            _nextBackgroundIndex = (_nextBackgroundIndex + 1) % loadingBackgrounds.Length;

            if (loadingBackgrounds[index] != null)
            {
                _lastBackgroundIndex = index;
                return loadingBackgrounds[index];
            }
        }

        return null;
    }

    private Sprite GetRandomLoadingBackground()
    {
        int availableCount = CountAvailableLoadingBackgrounds();
        if (availableCount == 0)
            return null;

        for (int i = 0; i < loadingBackgrounds.Length * 2; i++)
        {
            int index = Random.Range(0, loadingBackgrounds.Length);
            Sprite candidate = loadingBackgrounds[index];
            if (candidate == null)
                continue;

            if (availableCount > 1 && index == _lastBackgroundIndex)
                continue;

            _lastBackgroundIndex = index;
            return candidate;
        }

        for (int i = 0; i < loadingBackgrounds.Length; i++)
        {
            if (loadingBackgrounds[i] == null)
                continue;

            _lastBackgroundIndex = i;
            return loadingBackgrounds[i];
        }

        return null;
    }

    private int CountAvailableLoadingBackgrounds()
    {
        if (loadingBackgrounds == null)
            return 0;

        int count = 0;
        for (int i = 0; i < loadingBackgrounds.Length; i++)
        {
            if (loadingBackgrounds[i] != null)
                count++;
        }

        return count;
    }

    private IEnumerator FadeOverlay(SceneLoadingOverlay overlay, float targetAlpha, float durationSeconds)
    {
        if (overlay == null)
            yield break;

        float startAlpha = overlay.Alpha;
        float duration = Mathf.Max(0f, durationSeconds);

        if (Mathf.Approximately(duration, 0f))
        {
            overlay.SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            _sceneUiInputSuspender.Suspend();
            overlay.Tick();
            elapsed += Time.unscaledDeltaTime;
            overlay.SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        overlay.SetAlpha(targetAlpha);
    }

    private IEnumerator FadeCover(SceneLoadingOverlay overlay, float targetAlpha, float durationSeconds)
    {
        if (overlay == null)
            yield break;

        float startAlpha = overlay.CoverAlpha;
        float duration = Mathf.Max(0f, durationSeconds);

        if (Mathf.Approximately(duration, 0f))
        {
            overlay.SetCoverAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            _sceneUiInputSuspender.Suspend();
            elapsed += Time.unscaledDeltaTime;
            overlay.SetCoverAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        overlay.SetCoverAlpha(targetAlpha);
    }

    private IEnumerator HoldOverlay(SceneLoadingOverlay overlay, float durationSeconds)
    {
        if (overlay == null)
            yield break;

        float holdUntil = Time.realtimeSinceStartup + Mathf.Max(0f, durationSeconds);
        while (Time.realtimeSinceStartup < holdUntil)
        {
            _sceneUiInputSuspender.Suspend();
            yield return null;
        }
    }

    private IEnumerator FadeLoadingContent(SceneLoadingOverlay overlay, float targetAlpha, float durationSeconds)
    {
        if (overlay == null)
            yield break;

        float startAlpha = overlay.ContentAlpha;
        float duration = Mathf.Max(0f, durationSeconds);

        if (Mathf.Approximately(duration, 0f))
        {
            overlay.SetContentAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            _sceneUiInputSuspender.Suspend();
            elapsed += Time.unscaledDeltaTime;
            overlay.SetContentAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        overlay.SetContentAlpha(targetAlpha);
    }
}
