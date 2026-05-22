using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;
using PickingMode = UnityEngine.UIElements.PickingMode;
using UIDocument = UnityEngine.UIElements.UIDocument;
using VisualElement = UnityEngine.UIElements.VisualElement;

public sealed class SceneTransitionService : MonoBehaviour, IRunGateTransitionService
{
    public static SceneTransitionService Instance { get; private set; }

    private const string DefaultLoadingMessage = "Loading";
    private const int LoadingOverlaySortingOrder = short.MaxValue;

    [Header("Optional hooks (UI fade, SFX, etc.)")]
    public UnityEvent onTransitionStart;
    public UnityEvent onTransitionEnd;

    [Header("Loading Screen")]
    [SerializeField] private bool showLoadingScreen = true;
    [SerializeField] private Sprite[] loadingBackgrounds;
    [SerializeField] private bool randomizeLoadingBackgrounds = true;
    [SerializeField] private Color fallbackBackgroundColor = Color.black;
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
    [SerializeField] private Font loadingFont;
    [SerializeField, Min(1)] private int loadingFontSize = 48;
    [SerializeField] private Color loadingTextColor = Color.white;

    private bool _isLoading;
    private SceneLoadingOverlay _loadingOverlay;
    private readonly List<EventSystem> _suspendedEventSystems = new List<EventSystem>();
    private readonly Dictionary<VisualElement, PickingMode> _suspendedPickingModes = new Dictionary<VisualElement, PickingMode>();
    private int _nextBackgroundIndex;
    private int _lastBackgroundIndex = -1;

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

        ResumeSceneUiInput();
        _loadingOverlay?.Dispose();
        _loadingOverlay = null;
        Instance = null;
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

        SceneLoadingOverlay overlay = showLoadingScreen ? EnsureLoadingOverlay() : null;
        int resolvedActivationDotCount = Mathf.Max(1, activationDotCount);
        float resolvedDotInterval = Mathf.Max(0.05f, dotIntervalSeconds);
        float activationReadyTime = Time.realtimeSinceStartup;
        float minimumDisplayEndTime = Time.realtimeSinceStartup;

        AsyncOperation op;

        if (overlay != null)
        {
            SuspendSceneUiInput();

            overlay.Show(
                GetNextLoadingBackground(),
                fallbackBackgroundColor,
                backgroundDimAlpha,
                loadingMessage,
                resolvedDotInterval,
                resolvedActivationDotCount,
                loadingFont,
                loadingFontSize,
                loadingTextColor);

            yield return FadeCover(overlay, 1f, fadeInSeconds);
            overlay.ResetLoadingContent();

            op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
                overlay.Hide();
                ResumeSceneUiInput();
                onTransitionEnd?.Invoke();
                _isLoading = false;
                yield break;
            }

            op.allowSceneActivation = false;

            yield return HoldOverlay(overlay, blackHoldSeconds);
            yield return FadeLoadingContent(overlay, 1f, loadingContentFadeInSeconds);
            overlay.SetCoverAlpha(0f);
            yield return null;

            float loadingVisibleTime = Time.realtimeSinceStartup;
            activationReadyTime = loadingVisibleTime + resolvedDotInterval * resolvedActivationDotCount;
            minimumDisplayEndTime = loadingVisibleTime + Mathf.Max(0f, minimumDisplaySeconds);
        }
        else
        {
            op = SceneManager.LoadSceneAsync(sceneName, mode);
            if (op == null)
            {
                Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
                ResumeSceneUiInput();
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
                SuspendSceneUiInput();
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
                SuspendSceneUiInput();
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
                SuspendSceneUiInput();
                overlay.Tick();
                yield return null;
            }
        }

        if (overlay != null)
        {
            yield return FadeOverlay(overlay, 0f, fadeOutSeconds);
            overlay.Hide();
            ResumeSceneUiInput();
        }

        onTransitionEnd?.Invoke();
        _isLoading = false;
    }

    private SceneLoadingOverlay EnsureLoadingOverlay()
    {
        if (_loadingOverlay != null)
            return _loadingOverlay;

        _loadingOverlay = new SceneLoadingOverlay(LoadingOverlaySortingOrder);
        return _loadingOverlay;
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

    private void SuspendSceneUiInput()
    {
        SuspendActiveEventSystems();
        SuspendUiToolkitPicking();
    }

    private void ResumeSceneUiInput()
    {
        ResumeUiToolkitPicking();
        ResumeEventSystems();
    }

    private void SuspendActiveEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        for (int i = 0; i < eventSystems.Length; i++)
        {
            EventSystem eventSystem = eventSystems[i];
            if (eventSystem == null || !eventSystem.enabled)
                continue;

            if (!_suspendedEventSystems.Contains(eventSystem))
            {
                _suspendedEventSystems.Add(eventSystem);
            }

            eventSystem.enabled = false;
        }
    }

    private void ResumeEventSystems()
    {
        for (int i = 0; i < _suspendedEventSystems.Count; i++)
        {
            EventSystem eventSystem = _suspendedEventSystems[i];
            if (eventSystem != null)
            {
                eventSystem.enabled = true;
            }
        }

        _suspendedEventSystems.Clear();
    }

    private void SuspendUiToolkitPicking()
    {
        UIDocument[] documents = FindObjectsOfType<UIDocument>();
        for (int i = 0; i < documents.Length; i++)
        {
            UIDocument document = documents[i];
            if (document == null || document.rootVisualElement == null)
                continue;

            SuspendPickingRecursive(document.rootVisualElement);
        }
    }

    private void SuspendPickingRecursive(VisualElement element)
    {
        if (element == null)
            return;

        if (!_suspendedPickingModes.ContainsKey(element))
        {
            _suspendedPickingModes.Add(element, element.pickingMode);
        }

        element.pickingMode = PickingMode.Ignore;

        int childCount = element.childCount;
        for (int i = 0; i < childCount; i++)
        {
            SuspendPickingRecursive(element.ElementAt(i));
        }
    }

    private void ResumeUiToolkitPicking()
    {
        foreach (KeyValuePair<VisualElement, PickingMode> entry in _suspendedPickingModes)
        {
            if (entry.Key != null)
            {
                entry.Key.pickingMode = entry.Value;
            }
        }

        _suspendedPickingModes.Clear();
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
            SuspendSceneUiInput();
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
            SuspendSceneUiInput();
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
            SuspendSceneUiInput();
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
            SuspendSceneUiInput();
            elapsed += Time.unscaledDeltaTime;
            overlay.SetContentAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        overlay.SetContentAlpha(targetAlpha);
    }

    private sealed class SceneLoadingOverlay
    {
        private readonly Canvas canvas;
        private readonly CanvasGroup canvasGroup;
        private readonly CanvasGroup contentGroup;
        private readonly Image backgroundImage;
        private readonly AspectRatioFitter backgroundAspectFitter;
        private readonly Image dimmerImage;
        private readonly Image coverImage;
        private readonly Image inputBlockerImage;
        private readonly Text loadingLabel;

        private string message = DefaultLoadingMessage;
        private float dotIntervalSeconds = 0.35f;
        private int maxDotCount = 3;
        private int currentDotCount;
        private float nextDotTime;
        private bool animateDots;

        public float Alpha => canvasGroup.alpha;
        public float ContentAlpha => contentGroup.alpha;
        public float CoverAlpha => coverImage.color.a;

        public SceneLoadingOverlay(int sortingOrder)
        {
            GameObject canvasObject = new GameObject("SceneTransition_LoadingScreen");
            Object.DontDestroyOnLoad(canvasObject);

            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RectTransform contentRoot = CreateFullScreenContainer("LoadingContent", canvasObject.transform);
            contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
            contentGroup.alpha = 0f;
            contentGroup.blocksRaycasts = false;
            contentGroup.interactable = false;

            RectTransform backgroundViewport = CreateFullScreenContainer("BackgroundViewport", contentRoot);
            backgroundViewport.gameObject.AddComponent<RectMask2D>();

            backgroundImage = CreateCenteredImage("Background", backgroundViewport);
            backgroundAspectFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            backgroundAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundAspectFitter.enabled = false;

            dimmerImage = CreateFullScreenImage("Dimmer", contentRoot);
            loadingLabel = CreateLoadingLabel(contentRoot);
            coverImage = CreateFullScreenImage("TransitionCover", canvasObject.transform);
            coverImage.rectTransform.SetAsFirstSibling();
            inputBlockerImage = CreateFullScreenImage("InputBlocker", canvasObject.transform);
            inputBlockerImage.color = Color.clear;
            inputBlockerImage.raycastTarget = true;

            canvas.enabled = false;
        }

        public void Show(
            Sprite background,
            Color fallbackColor,
            float dimAlpha,
            string loadingMessage,
            float dotInterval,
            int activationDotCount,
            Font font,
            int fontSize,
            Color textColor)
        {
            message = string.IsNullOrWhiteSpace(loadingMessage)
                ? DefaultLoadingMessage
                : loadingMessage.Trim();

            dotIntervalSeconds = Mathf.Max(0.05f, dotInterval);
            maxDotCount = Mathf.Max(1, activationDotCount);
            currentDotCount = 0;
            animateDots = true;
            nextDotTime = Time.realtimeSinceStartup + dotIntervalSeconds;

            backgroundImage.sprite = background;
            backgroundImage.color = background != null ? Color.white : fallbackColor;
            backgroundImage.preserveAspect = false;

            if (background != null && background.rect.height > 0f)
            {
                CenterInParent(backgroundImage.rectTransform);
                backgroundAspectFitter.aspectRatio = background.rect.width / background.rect.height;
                backgroundAspectFitter.enabled = true;
            }
            else
            {
                backgroundAspectFitter.enabled = false;
                StretchToParent(backgroundImage.rectTransform);
            }

            dimmerImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(dimAlpha));
            coverImage.color = new Color(fallbackColor.r, fallbackColor.g, fallbackColor.b, 0f);

            loadingLabel.font = font != null ? font : ResolveDefaultFont();
            loadingLabel.fontSize = Mathf.Max(1, fontSize);
            loadingLabel.resizeTextMaxSize = Mathf.Max(1, fontSize);
            loadingLabel.color = textColor;
            loadingLabel.enabled = true;
            ApplyText();

            canvas.enabled = true;
            canvasGroup.alpha = 1f;
            contentGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            inputBlockerImage.raycastTarget = true;
            Canvas.ForceUpdateCanvases();
        }

        public void ResetLoadingContent()
        {
            currentDotCount = 0;
            animateDots = true;
            nextDotTime = Time.realtimeSinceStartup + dotIntervalSeconds;
            loadingLabel.enabled = true;
            ApplyText();

            contentGroup.alpha = 0f;
        }

        public void Tick()
        {
            if (!canvas.enabled || !animateDots)
                return;

            while (Time.realtimeSinceStartup >= nextDotTime)
            {
                currentDotCount = currentDotCount >= maxDotCount ? 1 : currentDotCount + 1;
                nextDotTime += dotIntervalSeconds;
                ApplyText();
            }
        }

        public void CompleteLoading()
        {
            animateDots = false;
            currentDotCount = maxDotCount;
            ApplyText();
            loadingLabel.enabled = false;
        }

        public void SetAlpha(float alpha)
        {
            canvasGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetContentAlpha(float alpha)
        {
            contentGroup.alpha = Mathf.Clamp01(alpha);
        }

        public void SetCoverAlpha(float alpha)
        {
            Color color = coverImage.color;
            color.a = Mathf.Clamp01(alpha);
            coverImage.color = color;
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
            contentGroup.alpha = 0f;
            SetCoverAlpha(0f);
            animateDots = false;
            loadingLabel.enabled = false;
            inputBlockerImage.raycastTarget = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvas.enabled = false;
        }

        public void Dispose()
        {
            if (canvas != null)
            {
                Object.Destroy(canvas.gameObject);
            }
        }

        private void ApplyText()
        {
            loadingLabel.text = $"{message}{new string('.', currentDotCount)}";
        }

        private static RectTransform CreateFullScreenContainer(string name, Transform parent)
        {
            GameObject containerObject = new GameObject(name);
            containerObject.transform.SetParent(parent, false);

            RectTransform rect = containerObject.AddComponent<RectTransform>();
            StretchToParent(rect);

            return rect;
        }

        private static Image CreateFullScreenImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;

            StretchToParent(image.rectTransform);

            return image;
        }

        private static Image CreateCenteredImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(name);
            imageObject.transform.SetParent(parent, false);

            Image image = imageObject.AddComponent<Image>();
            image.raycastTarget = false;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;

            return image;
        }

        private static void CenterInParent(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static void StretchToParent(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateLoadingLabel(Transform parent)
        {
            GameObject labelObject = new GameObject("LoadingLabel");
            labelObject.transform.SetParent(parent, false);

            Text label = labelObject.AddComponent<Text>();
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 24;
            label.raycastTarget = false;

            Shadow shadow = labelObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(2f, -2f);

            RectTransform rect = label.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.16f);
            rect.anchorMax = new Vector2(0.5f, 0.16f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(760f, 96f);
            rect.anchoredPosition = Vector2.zero;

            return label;
        }

        private static Font ResolveDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
