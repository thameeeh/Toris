using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.UI;

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
    [SerializeField] private Color fallbackBackgroundColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float backgroundDimAlpha = 0.35f;
    [SerializeField] private string loadingMessage = DefaultLoadingMessage;
    [SerializeField, Min(0.05f)] private float dotIntervalSeconds = 0.35f;
    [SerializeField, Min(1)] private int activationDotCount = 3;
    [SerializeField, Min(0f)] private float postLoadHoldSeconds = 0.15f;
    [SerializeField] private Font loadingFont;
    [SerializeField, Min(1)] private int loadingFontSize = 48;
    [SerializeField] private Color loadingTextColor = Color.white;

    private bool _isLoading;
    private SceneLoadingOverlay _loadingOverlay;
    private int _nextBackgroundIndex;

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
        float activationReadyTime = Time.realtimeSinceStartup;
        int resolvedActivationDotCount = Mathf.Max(1, activationDotCount);
        float resolvedDotInterval = Mathf.Max(0.05f, dotIntervalSeconds);

        if (overlay != null)
        {
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

            activationReadyTime += resolvedDotInterval * resolvedActivationDotCount;
        }

        yield return null;

        var op = SceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            Debug.LogError($"[SceneTransitionService] Failed to load scene '{sceneName}'.");
            overlay?.Hide();
            onTransitionEnd?.Invoke();
            _isLoading = false;
            yield break;
        }

        if (overlay != null)
        {
            op.allowSceneActivation = false;

            while (op.progress < 0.9f || Time.realtimeSinceStartup < activationReadyTime)
            {
                overlay.Tick();
                yield return null;
            }

            overlay.SetDotCount(resolvedActivationDotCount);
            op.allowSceneActivation = true;
        }

        while (!op.isDone)
        {
            overlay?.Tick();
            yield return null;
        }

        yield return null;

        if (overlay != null && postLoadHoldSeconds > 0f)
        {
            float hideAt = Time.realtimeSinceStartup + postLoadHoldSeconds;
            while (Time.realtimeSinceStartup < hideAt)
            {
                overlay.Tick();
                yield return null;
            }
        }

        overlay?.Hide();

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

        for (int i = 0; i < loadingBackgrounds.Length; i++)
        {
            int index = _nextBackgroundIndex % loadingBackgrounds.Length;
            _nextBackgroundIndex = (_nextBackgroundIndex + 1) % loadingBackgrounds.Length;

            if (loadingBackgrounds[index] != null)
                return loadingBackgrounds[index];
        }

        return null;
    }

    private sealed class SceneLoadingOverlay
    {
        private readonly Canvas canvas;
        private readonly CanvasGroup canvasGroup;
        private readonly Image backgroundImage;
        private readonly AspectRatioFitter backgroundAspectFitter;
        private readonly Image dimmerImage;
        private readonly Text loadingLabel;

        private string message = DefaultLoadingMessage;
        private float dotIntervalSeconds = 0.35f;
        private int maxDotCount = 3;
        private int currentDotCount;
        private float nextDotTime;

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

            RectTransform backgroundViewport = CreateFullScreenContainer("BackgroundViewport", canvasObject.transform);
            backgroundViewport.gameObject.AddComponent<RectMask2D>();

            backgroundImage = CreateCenteredImage("Background", backgroundViewport);
            backgroundAspectFitter = backgroundImage.gameObject.AddComponent<AspectRatioFitter>();
            backgroundAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            backgroundAspectFitter.enabled = false;

            dimmerImage = CreateFullScreenImage("Dimmer", canvasObject.transform);
            loadingLabel = CreateLoadingLabel(canvasObject.transform);

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

            loadingLabel.font = font != null ? font : ResolveDefaultFont();
            loadingLabel.fontSize = Mathf.Max(1, fontSize);
            loadingLabel.resizeTextMaxSize = Mathf.Max(1, fontSize);
            loadingLabel.color = textColor;
            ApplyText();

            canvas.enabled = true;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            Canvas.ForceUpdateCanvases();
        }

        public void Tick()
        {
            if (!canvas.enabled)
                return;

            while (Time.realtimeSinceStartup >= nextDotTime)
            {
                currentDotCount = currentDotCount >= maxDotCount ? 0 : currentDotCount + 1;
                nextDotTime += dotIntervalSeconds;
                ApplyText();
            }
        }

        public void SetDotCount(int dotCount)
        {
            currentDotCount = Mathf.Clamp(dotCount, 0, maxDotCount);
            ApplyText();
        }

        public void Hide()
        {
            canvasGroup.alpha = 0f;
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
