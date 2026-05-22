using System;
using System.Collections;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public sealed class SceneTransitionService : MonoBehaviour, IRunGateTransitionService
{
    public static SceneTransitionService Instance { get; private set; }

    private const string DefaultLoadingMessage = "Loading";
    private const string MainMenuSceneName = "MainMenu";
    private const string MainAreaSceneName = "MainArea";
    private const string ProceduralTilesSceneName = "ProceduralTiles";
    private const string EnteringWorldMessage = "Entering the World";
    private const string EnteringOutlandsMessage = "Entering the Outlands";
    private const string ComingBackMessage = "Coming Back";
    private const string LeavingMessage = "Leaving";
    private const float MinDotIntervalSeconds = 0.05f;
    private const float MinReadyTimeoutSeconds = 0.1f;

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
    [SerializeField, Min(0f)] private float postLoadHoldSeconds = 0.15f;

    [Header("Loading Timing Variation")]
    [SerializeField] private Vector2 minimumDisplaySecondsRange = new Vector2(2f, 4f);
    [SerializeField, Min(1f)] private float timingLowerBiasPower = 2.4f;
    [SerializeField] private Vector2 fadeInSecondsRange = new Vector2(0.3f, 0.45f);
    [SerializeField] private Vector2 blackHoldSecondsRange = new Vector2(0.12f, 0.22f);
    [SerializeField] private Vector2 loadingContentFadeInSecondsRange = new Vector2(0.3f, 0.45f);
    [SerializeField] private Vector2 fadeOutSecondsRange = new Vector2(0.3f, 0.45f);

    [Header("Gameplay Input")]
    [SerializeField] private UIEventsSO uiEvents;
    [SerializeField] private string gameplayInputLockId = "SceneTransitionLoading";

    private readonly SceneUiInputSuspender _sceneUiInputSuspender = new SceneUiInputSuspender();
    private bool _isLoading;
    private bool _gameplayInputLocked;
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
        UnlockGameplayInput();
        _loadingOverlay?.Dispose();
        _loadingOverlay = null;
        Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        LoadScene(sceneName, LoadSceneMode.Single);
    }

    public void LoadScene(string sceneName, string loadingMessageOverride)
    {
        LoadScene(sceneName, LoadSceneMode.Single, loadingMessageOverride);
    }

    public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        LoadScene(sceneName, mode, null);
    }

    public void LoadScene(string sceneName, LoadSceneMode mode, string loadingMessageOverride)
    {
        if (_isLoading)
            return;

        StartCoroutine(LoadRoutine(sceneName, mode, loadingMessageOverride));
    }

    public bool TryRunLoadingTransition(
        string transitionName,
        Action coveredWork,
        Func<bool> isReadyForReveal,
        float readyTimeoutSeconds,
        float postReadyHoldSeconds,
        string loadingMessageOverride = null)
    {
        if (_isLoading)
            return false;

        StartCoroutine(LoadingTransitionRoutine(
            transitionName,
            coveredWork,
            isReadyForReveal,
            readyTimeoutSeconds,
            postReadyHoldSeconds,
            loadingMessageOverride));
        return true;
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

    private LoadingTransitionSession BeginLoadingTransition(string resolvedLoadingMessage)
    {
        _isLoading = true;
        onTransitionStart?.Invoke();
        LockGameplayInput();

        SceneLoadingOverlay overlay = showLoadingScreen ? EnsureLoadingOverlay() : null;
        return new LoadingTransitionSession(
            overlay,
            CreateLoadingTransitionTiming(),
            Mathf.Max(1, activationDotCount),
            Mathf.Max(MinDotIntervalSeconds, dotIntervalSeconds),
            ResolveLoadingMessage(resolvedLoadingMessage));
    }

    private void EndLoadingTransition()
    {
        UnlockGameplayInput();
        onTransitionEnd?.Invoke();
        _isLoading = false;
    }

    private void AbortLoadingTransition(LoadingTransitionSession session)
    {
        if (session.HasOverlay)
        {
            session.Overlay.Hide();
            _sceneUiInputSuspender.Resume();
        }

        EndLoadingTransition();
    }

    private IEnumerator CoverWithLoadingOverlay(LoadingTransitionSession session)
    {
        if (!session.HasOverlay)
            yield break;

        _sceneUiInputSuspender.Suspend();
        session.Overlay.Show(CreateLoadingOverlaySettings(
            session.DotIntervalSeconds,
            session.ActivationDotCount,
            session.LoadingMessage));

        yield return FadeCover(session.Overlay, 1f, session.Timing.FadeInSeconds);
        session.Overlay.ResetLoadingContent();
    }

    private IEnumerator RevealLoadingContent(LoadingTransitionSession session)
    {
        if (!session.HasOverlay)
            yield break;

        yield return HoldOverlay(session.Overlay, session.Timing.BlackHoldSeconds);
        yield return FadeLoadingContent(session.Overlay, 1f, session.Timing.LoadingContentFadeInSeconds);
        session.Overlay.SetCoverAlpha(0f);
        session.Overlay.StartLoadingAnimation();
        yield return null;
    }

    private static void ResolveLoadingGateTimes(
        LoadingTransitionSession session,
        out float activationReadyTime,
        out float minimumDisplayEndTime)
    {
        float loadingVisibleTime = Time.realtimeSinceStartup;
        activationReadyTime = loadingVisibleTime
                              + session.DotIntervalSeconds * Mathf.Max(0, session.ActivationDotCount - 1);
        minimumDisplayEndTime = loadingVisibleTime + session.Timing.MinimumDisplaySeconds;
    }

    private IEnumerator HideLoadingOverlay(LoadingTransitionSession session, float holdSeconds)
    {
        if (!session.HasOverlay)
            yield break;

        yield return TickOverlayForDuration(session.Overlay, holdSeconds);
        yield return FadeOverlay(session.Overlay, 0f, session.Timing.FadeOutSeconds);
        session.Overlay.Hide();
        _sceneUiInputSuspender.Resume();
    }

    private IEnumerator TickOverlayForDuration(SceneLoadingOverlay overlay, float durationSeconds)
    {
        if (overlay == null || durationSeconds <= 0f)
            yield break;

        float endTime = Time.realtimeSinceStartup + durationSeconds;
        while (Time.realtimeSinceStartup < endTime)
        {
            _sceneUiInputSuspender.Suspend();
            overlay.Tick();
            yield return null;
        }
    }

    private IEnumerator LoadRoutine(string sceneName, LoadSceneMode mode, string loadingMessageOverride)
    {
        string resolvedLoadingMessage = string.IsNullOrWhiteSpace(loadingMessageOverride)
            ? ResolveSceneLoadingMessage(SceneManager.GetActiveScene().name, sceneName)
            : loadingMessageOverride;

        LoadingTransitionSession session = BeginLoadingTransition(
            resolvedLoadingMessage);
        float activationReadyTime = Time.realtimeSinceStartup;
        float minimumDisplayEndTime = Time.realtimeSinceStartup;
        AsyncOperation op;

        if (session.HasOverlay)
        {
            yield return CoverWithLoadingOverlay(session);

            op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
                AbortLoadingTransition(session);
                yield break;
            }

            op.allowSceneActivation = false;

            yield return RevealLoadingContent(session);
            ResolveLoadingGateTimes(session, out activationReadyTime, out minimumDisplayEndTime);
        }
        else
        {
            op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
                AbortLoadingTransition(session);
                yield break;
            }
        }

        if (session.HasOverlay)
        {
            while (op.progress < 0.9f
                   || Time.realtimeSinceStartup < activationReadyTime
                   || Time.realtimeSinceStartup < minimumDisplayEndTime)
            {
                _sceneUiInputSuspender.Suspend();
                session.Overlay.Tick();
                yield return null;
            }

            session.Overlay.CompleteLoading();
            op.allowSceneActivation = true;
        }

        while (!op.isDone)
        {
            if (session.HasOverlay)
            {
                _sceneUiInputSuspender.Suspend();
            }

            session.Overlay?.Tick();
            yield return null;
        }

        yield return null;

        yield return HideLoadingOverlay(session, postLoadHoldSeconds);
        EndLoadingTransition();
    }

    private IEnumerator LoadingTransitionRoutine(
        string transitionName,
        Action coveredWork,
        Func<bool> isReadyForReveal,
        float readyTimeoutSeconds,
        float postReadyHoldSeconds,
        string loadingMessageOverride)
    {
        LoadingTransitionSession session = BeginLoadingTransition(loadingMessageOverride);
        float resolvedReadyTimeout = Mathf.Max(MinReadyTimeoutSeconds, readyTimeoutSeconds);
        bool workStarted;
        float readyTimeoutTime;

        if (session.HasOverlay)
        {
            yield return CoverWithLoadingOverlay(session);

            // Cross-system handoff: the overlay is opaque before non-scene transition work mutates the world.
            workStarted = TryRunCoveredWork(transitionName, coveredWork);
            readyTimeoutTime = Time.realtimeSinceStartup + resolvedReadyTimeout;

            yield return RevealLoadingContent(session);
            ResolveLoadingGateTimes(session, out float activationReadyTime, out float minimumDisplayEndTime);

            while (Time.realtimeSinceStartup < activationReadyTime
                   || Time.realtimeSinceStartup < minimumDisplayEndTime
                   || !IsReadyForReveal(workStarted, isReadyForReveal))
            {
                if (workStarted
                    && Time.realtimeSinceStartup >= readyTimeoutTime
                    && Time.realtimeSinceStartup >= activationReadyTime
                    && Time.realtimeSinceStartup >= minimumDisplayEndTime)
                {
                    Debug.LogWarning(
                        $"[SceneTransitionService] Loading transition '{transitionName}' timed out waiting for reveal readiness.",
                        this);
                    break;
                }

                _sceneUiInputSuspender.Suspend();
                session.Overlay.Tick();
                yield return null;
            }

            session.Overlay.CompleteLoading();
            yield return HideLoadingOverlay(session, postReadyHoldSeconds);
        }
        else
        {
            workStarted = TryRunCoveredWork(transitionName, coveredWork);
            readyTimeoutTime = Time.realtimeSinceStartup + resolvedReadyTimeout;

            while (workStarted && !IsReadyForReveal(workStarted, isReadyForReveal))
            {
                if (Time.realtimeSinceStartup >= readyTimeoutTime)
                    break;

                yield return null;
            }
        }

        EndLoadingTransition();
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
        int resolvedActivationDotCount,
        string resolvedLoadingMessage)
    {
        return new SceneLoadingOverlaySettings
        {
            Background = GetNextLoadingBackground(),
            FallbackColor = fallbackBackgroundColor,
            BackgroundOverscan = loadingBackgroundOverscan,
            DimAlpha = backgroundDimAlpha,
            LoadingMessage = resolvedLoadingMessage,
            DotIntervalSeconds = resolvedDotInterval,
            ActivationDotCount = resolvedActivationDotCount
        };
    }

    private string ResolveSceneLoadingMessage(string currentSceneName, string targetSceneName)
    {
        if (SceneNameEquals(targetSceneName, MainMenuSceneName))
            return LeavingMessage;

        if (SceneNameEquals(currentSceneName, MainMenuSceneName)
            && SceneNameEquals(targetSceneName, MainAreaSceneName))
        {
            return EnteringWorldMessage;
        }

        if (SceneNameEquals(currentSceneName, MainAreaSceneName)
            && SceneNameEquals(targetSceneName, ProceduralTilesSceneName))
        {
            return EnteringOutlandsMessage;
        }

        if (SceneNameEquals(currentSceneName, ProceduralTilesSceneName)
            && SceneNameEquals(targetSceneName, MainAreaSceneName))
        {
            return ComingBackMessage;
        }

        return loadingMessage;
    }

    private string ResolveLoadingMessage(string messageOverride)
    {
        string resolvedMessage = string.IsNullOrWhiteSpace(messageOverride)
            ? loadingMessage
            : messageOverride;

        return string.IsNullOrWhiteSpace(resolvedMessage)
            ? DefaultLoadingMessage
            : resolvedMessage.Trim().TrimEnd('.');
    }

    private static bool SceneNameEquals(string a, string b)
    {
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
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
            int index = UnityEngine.Random.Range(0, loadingBackgrounds.Length);
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

    private bool TryRunCoveredWork(string transitionName, Action coveredWork)
    {
        try
        {
            coveredWork?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogException(
                new InvalidOperationException(
                    $"Scene loading transition '{transitionName}' failed during covered work.",
                    exception),
                this);
            return false;
        }
    }

    private static bool IsReadyForReveal(bool workStarted, Func<bool> isReadyForReveal)
    {
        return !workStarted || isReadyForReveal == null || isReadyForReveal();
    }

    private void LockGameplayInput()
    {
        if (uiEvents == null || _gameplayInputLocked || string.IsNullOrWhiteSpace(gameplayInputLockId))
            return;

        // Input-system handoff: InputManager owns suppression; transitions only raise lock events.
        uiEvents.OnGameplayInputLockRequested?.Invoke(gameplayInputLockId);
        _gameplayInputLocked = true;
    }

    private void UnlockGameplayInput()
    {
        if (uiEvents == null || !_gameplayInputLocked || string.IsNullOrWhiteSpace(gameplayInputLockId))
            return;

        uiEvents.OnGameplayInputUnlockRequested?.Invoke(gameplayInputLockId);
        _gameplayInputLocked = false;
    }

    private LoadingTransitionTiming CreateLoadingTransitionTiming()
    {
        float resolvedBias = Mathf.Max(1f, timingLowerBiasPower);
        return new LoadingTransitionTiming(
            ResolveWeightedTiming(fadeInSecondsRange, resolvedBias),
            ResolveWeightedTiming(blackHoldSecondsRange, resolvedBias),
            ResolveWeightedTiming(loadingContentFadeInSecondsRange, resolvedBias),
            ResolveWeightedTiming(fadeOutSecondsRange, resolvedBias),
            ResolveWeightedTiming(minimumDisplaySecondsRange, resolvedBias));
    }

    private static float ResolveWeightedTiming(Vector2 range, float lowerBiasPower)
    {
        float min = Mathf.Max(0f, Mathf.Min(range.x, range.y));
        float max = Mathf.Max(min, Mathf.Max(range.x, range.y));

        if (Mathf.Approximately(min, max))
            return min;

        float t = Mathf.Pow(UnityEngine.Random.value, lowerBiasPower);
        return Mathf.Lerp(min, max, t);
    }

    private struct LoadingTransitionTiming
    {
        public readonly float FadeInSeconds;
        public readonly float BlackHoldSeconds;
        public readonly float LoadingContentFadeInSeconds;
        public readonly float FadeOutSeconds;
        public readonly float MinimumDisplaySeconds;

        public LoadingTransitionTiming(
            float fadeInSeconds,
            float blackHoldSeconds,
            float loadingContentFadeInSeconds,
            float fadeOutSeconds,
            float minimumDisplaySeconds)
        {
            FadeInSeconds = fadeInSeconds;
            BlackHoldSeconds = blackHoldSeconds;
            LoadingContentFadeInSeconds = loadingContentFadeInSeconds;
            FadeOutSeconds = fadeOutSeconds;
            MinimumDisplaySeconds = minimumDisplaySeconds;
        }
    }

    private struct LoadingTransitionSession
    {
        public readonly SceneLoadingOverlay Overlay;
        public readonly LoadingTransitionTiming Timing;
        public readonly int ActivationDotCount;
        public readonly float DotIntervalSeconds;
        public readonly string LoadingMessage;

        public bool HasOverlay => Overlay != null;

        public LoadingTransitionSession(
            SceneLoadingOverlay overlay,
            LoadingTransitionTiming timing,
            int activationDotCount,
            float dotIntervalSeconds,
            string loadingMessage)
        {
            Overlay = overlay;
            Timing = timing;
            ActivationDotCount = activationDotCount;
            DotIntervalSeconds = dotIntervalSeconds;
            LoadingMessage = loadingMessage;
        }
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
